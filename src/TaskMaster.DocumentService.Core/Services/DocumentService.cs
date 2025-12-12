using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Interfaces;

namespace TaskMaster.DocumentService.Core.Services;

/// <summary>
/// Implementation of document management service with business logic for CRUD operations, versioning, and metadata management.
/// </summary>
public class DocumentService : IDocumentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IDocumentIndexer? _documentIndexer;
    private readonly ILogger<DocumentService> _logger;
    private readonly BlobStorageOptions _blobStorageOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentService"/> class.
    /// </summary>
    /// <param name="unitOfWork">The unit of work for database operations.</param>
    /// <param name="blobStorageService">The blob storage service.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="blobStorageOptions">The blob storage configuration options.</param>
    /// <param name="documentIndexer">Optional document indexer for search integration.</param>
    public DocumentService(
        IUnitOfWork unitOfWork,
        IBlobStorageService blobStorageService,
        ILogger<DocumentService> logger,
        IOptions<BlobStorageOptions> blobStorageOptions,
        IDocumentIndexer? documentIndexer = null)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _blobStorageService = blobStorageService ?? throw new ArgumentNullException(nameof(blobStorageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _blobStorageOptions = blobStorageOptions?.Value ?? throw new ArgumentNullException(nameof(blobStorageOptions));
        _documentIndexer = documentIndexer; // Optional - null if search not configured
    }

    /// <inheritdoc/>
    public async Task<Document> CreateDocumentAsync(
        int tenantId,
        int documentTypeId,
        string title,
        string? description,
        Stream content,
        string fileName,
        string contentType,
        string? metadata,
        string? tags,
        string createdBy,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be null or empty.", nameof(title));

        if (content == null)
            throw new ArgumentNullException(nameof(content));

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));

        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ArgumentException("Created by cannot be null or empty.", nameof(createdBy));

        try
        {
            _logger.LogInformation(
                "Creating document '{Title}' for tenant {TenantId} by user {CreatedBy}",
                title, tenantId, createdBy);

            // Validate tenant exists
            var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
            if (tenant == null)
            {
                throw new InvalidOperationException($"Tenant with ID {tenantId} not found.");
            }

            // Validate document type exists
            var documentType = await _unitOfWork.DocumentTypes.GetByIdAsync(documentTypeId, cancellationToken);
            if (documentType == null)
            {
                throw new InvalidOperationException($"Document type with ID {documentTypeId} not found.");
            }

            // Calculate content hash and file size
            var contentHash = await ComputeContentHashAsync(content, cancellationToken);
            var fileSize = content.Length;
            content.Position = 0; // Reset stream position after hash calculation

            // Generate unique blob name
            var blobName = GenerateBlobName(tenantId, fileName);

            // Upload to blob storage
            var blobUri = await _blobStorageService.UploadAsync(
                _blobStorageOptions.DefaultContainerName,
                blobName,
                content,
                contentType,
                cancellationToken);

            // Create document entity
            var document = new Document
            {
                TenantId = tenantId,
                DocumentTypeId = documentTypeId,
                Title = title,
                Description = description,
                BlobPath = blobName,
                ContentHash = contentHash,
                FileSizeBytes = fileSize,
                MimeType = contentType,
                OriginalFileName = fileName,
                Metadata = metadata,
                Tags = tags,
                Version = 1,
                IsCurrentVersion = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                IsDeleted = false,
                IsArchived = false
            };

            await _unitOfWork.Documents.AddAsync(document, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Index document for search if indexer is configured and document type is indexable
            if (_documentIndexer != null && documentType.IsContentIndexed)
            {
                try
                {
                    var meilisearchId = await _documentIndexer.IndexDocumentAsync(document, cancellationToken);
                    if (!string.IsNullOrEmpty(meilisearchId))
                    {
                        document.MeilisearchId = meilisearchId;
                        document.LastIndexedAt = DateTime.UtcNow;
                        _unitOfWork.Documents.Update(document);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        
                        _logger.LogInformation(
                            "Document {DocumentId} indexed with MeilisearchId {MeilisearchId}",
                            document.Id, meilisearchId);
                    }
                }
                catch (Exception indexEx)
                {
                    // Log but don't fail document creation if indexing fails
                    _logger.LogWarning(indexEx, 
                        "Failed to index document {DocumentId}, will be retried by background service",
                        document.Id);
                }
            }

            _logger.LogInformation(
                "Successfully created document {DocumentId} with title '{Title}'",
                document.Id, title);

            return document;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create document '{Title}' for tenant {TenantId}", title, tenantId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Document?> GetDocumentByIdAsync(long documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving document {DocumentId}", documentId);

            var document = await _unitOfWork.Documents.GetByIdAsync(documentId, cancellationToken);

            if (document == null)
            {
                _logger.LogWarning("Document {DocumentId} not found", documentId);
            }

            return document;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve document {DocumentId}", documentId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Document>> GetDocumentsByTenantAsync(
        int tenantId,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving documents for tenant {TenantId}, includeDeleted: {IncludeDeleted}",
                tenantId, includeDeleted);

            return await _unitOfWork.Documents.GetByTenantIdAsync(tenantId, includeDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve documents for tenant {TenantId}", tenantId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Document>> GetDocumentsByTypeAsync(
        int documentTypeId,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving documents for document type {DocumentTypeId}, includeDeleted: {IncludeDeleted}",
                documentTypeId, includeDeleted);

            return await _unitOfWork.Documents.GetByDocumentTypeIdAsync(documentTypeId, includeDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve documents for document type {DocumentTypeId}", documentTypeId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Document> UpdateDocumentMetadataAsync(
        long documentId,
        string? title,
        string? description,
        string? metadata,
        string? tags,
        string updatedBy,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(updatedBy))
            throw new ArgumentException("Updated by cannot be null or empty.", nameof(updatedBy));

        try
        {
            _logger.LogInformation("Updating metadata for document {DocumentId} by user {UpdatedBy}",
                documentId, updatedBy);

            var document = await _unitOfWork.Documents.GetByIdAsync(documentId, cancellationToken);
            if (document == null)
            {
                throw new InvalidOperationException($"Document with ID {documentId} not found.");
            }

            if (document.IsDeleted)
            {
                throw new InvalidOperationException($"Cannot update metadata of deleted document {documentId}.");
            }

            // Update properties if provided
            if (!string.IsNullOrWhiteSpace(title))
            {
                document.Title = title;
            }

            if (description != null)
            {
                document.Description = description;
            }

            if (metadata != null)
            {
                document.Metadata = metadata;
            }

            if (tags != null)
            {
                document.Tags = tags;
            }

            document.UpdatedAt = DateTime.UtcNow;
            document.UpdatedBy = updatedBy;

            _unitOfWork.Documents.Update(document);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully updated metadata for document {DocumentId}", documentId);

            return document;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update metadata for document {DocumentId}", documentId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Document> CreateDocumentVersionAsync(
        long parentDocumentId,
        Stream content,
        string fileName,
        string contentType,
        string updatedBy,
        CancellationToken cancellationToken = default)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));

        if (string.IsNullOrWhiteSpace(updatedBy))
            throw new ArgumentException("Updated by cannot be null or empty.", nameof(updatedBy));

        try
        {
            _logger.LogInformation(
                "Creating new version for document {ParentDocumentId} by user {UpdatedBy}",
                parentDocumentId, updatedBy);

            // Calculate content hash and file size before entering the transaction
            var contentHash = await ComputeContentHashAsync(content, cancellationToken);
            var fileSize = content.Length;
            content.Position = 0; // Reset stream position

            // Execute the versioning logic within an execution strategy to support retry on transient failures
            return await _unitOfWork.ExecuteInTransactionAsync(async (ct) =>
            {
                // Get parent document
                var parentDocument = await _unitOfWork.Documents.GetByIdAsync(parentDocumentId, ct);
                if (parentDocument == null)
                {
                    throw new InvalidOperationException($"Parent document with ID {parentDocumentId} not found.");
                }

                if (parentDocument.IsDeleted)
                {
                    throw new InvalidOperationException($"Cannot create version of deleted document {parentDocumentId}.");
                }

                // Check if content is the same as current version
                if (contentHash == parentDocument.ContentHash)
                {
                    _logger.LogWarning(
                        "New version has same content hash as parent document {ParentDocumentId}. Skipping version creation.",
                        parentDocumentId);
                    return parentDocument;
                }

                // Get the highest version number
                var versions = await _unitOfWork.Documents.GetVersionsAsync(parentDocumentId, ct);
                var maxVersion = versions.Any() ? versions.Max(v => v.Version) : parentDocument.Version;
                var newVersionNumber = maxVersion + 1;

                // Generate unique blob name for new version
                var blobName = GenerateBlobName(parentDocument.TenantId, fileName, newVersionNumber);

                // Upload to blob storage
                var blobUri = await _blobStorageService.UploadAsync(
                    _blobStorageOptions.DefaultContainerName,
                    blobName,
                    content,
                    contentType,
                    ct);

                // Mark all previous versions as not current
                var currentVersion = await _unitOfWork.Documents.GetCurrentVersionAsync(parentDocumentId, ct);
                if (currentVersion != null)
                {
                    currentVersion.IsCurrentVersion = false;
                    _unitOfWork.Documents.Update(currentVersion);
                }

                // Also mark parent as not current
                parentDocument.IsCurrentVersion = false;
                _unitOfWork.Documents.Update(parentDocument);

                // Create new version
                var newVersion = new Document
                {
                    TenantId = parentDocument.TenantId,
                    DocumentTypeId = parentDocument.DocumentTypeId,
                    Title = parentDocument.Title,
                    Description = parentDocument.Description,
                    BlobPath = blobName,
                    ContentHash = contentHash,
                    FileSizeBytes = fileSize,
                    MimeType = contentType,
                    OriginalFileName = fileName,
                    Metadata = parentDocument.Metadata,
                    Tags = parentDocument.Tags,
                    Version = newVersionNumber,
                    ParentDocumentId = parentDocumentId,
                    IsCurrentVersion = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = updatedBy,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = updatedBy,
                    IsDeleted = false,
                    IsArchived = false
                };

                await _unitOfWork.Documents.AddAsync(newVersion, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "Successfully created version {Version} for document {ParentDocumentId}",
                    newVersionNumber, parentDocumentId);

                return newVersion;
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create version for document {ParentDocumentId}", parentDocumentId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Document>> GetDocumentVersionsAsync(
        long parentDocumentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving versions for document {ParentDocumentId}", parentDocumentId);

            return await _unitOfWork.Documents.GetVersionsAsync(parentDocumentId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve versions for document {ParentDocumentId}", parentDocumentId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Document?> GetCurrentVersionAsync(
        long parentDocumentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving current version for document {ParentDocumentId}", parentDocumentId);

            return await _unitOfWork.Documents.GetCurrentVersionAsync(parentDocumentId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve current version for document {ParentDocumentId}", parentDocumentId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Stream> DownloadDocumentAsync(long documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Downloading document {DocumentId}", documentId);

            var document = await _unitOfWork.Documents.GetByIdAsync(documentId, cancellationToken);
            if (document == null)
            {
                throw new InvalidOperationException($"Document with ID {documentId} not found.");
            }

            if (document.IsDeleted)
            {
                throw new InvalidOperationException($"Cannot download deleted document {documentId}.");
            }

            var stream = await _blobStorageService.DownloadAsync(
                _blobStorageOptions.DefaultContainerName,
                document.BlobPath,
                cancellationToken);

            _logger.LogInformation("Successfully downloaded document {DocumentId}", documentId);

            return stream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download document {DocumentId}", documentId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string> GetDocumentSasUriAsync(
        long documentId,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating SAS URI for document {DocumentId}", documentId);

            var document = await _unitOfWork.Documents.GetByIdAsync(documentId, cancellationToken);
            if (document == null)
            {
                throw new InvalidOperationException($"Document with ID {documentId} not found.");
            }

            if (document.IsDeleted)
            {
                throw new InvalidOperationException($"Cannot generate SAS URI for deleted document {documentId}.");
            }

            var sasUri = await _blobStorageService.GetSasUriAsync(
                _blobStorageOptions.DefaultContainerName,
                document.BlobPath,
                expiresIn,
                cancellationToken);

            _logger.LogInformation("Successfully generated SAS URI for document {DocumentId}", documentId);

            return sasUri;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate SAS URI for document {DocumentId}", documentId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task DeleteDocumentAsync(
        long documentId,
        string deletedBy,
        string? deletedReason = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deletedBy))
            throw new ArgumentException("Deleted by cannot be null or empty.", nameof(deletedBy));

        try
        {
            _logger.LogInformation(
                "Soft deleting document {DocumentId} by user {DeletedBy}",
                documentId, deletedBy);

            await _unitOfWork.Documents.SoftDeleteAsync(documentId, deletedBy, deletedReason, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully soft deleted document {DocumentId}", documentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to soft delete document {DocumentId}", documentId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task RestoreDocumentAsync(long documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Restoring document {DocumentId}", documentId);

            await _unitOfWork.Documents.RestoreAsync(documentId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully restored document {DocumentId}", documentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore document {DocumentId}", documentId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task ArchiveDocumentAsync(long documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Archiving document {DocumentId}", documentId);

            await _unitOfWork.Documents.ArchiveAsync(documentId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully archived document {DocumentId}", documentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to archive document {DocumentId}", documentId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Document>> GetArchivedDocumentsAsync(
        int tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving archived documents for tenant {TenantId}", tenantId);

            return await _unitOfWork.Documents.GetArchivedDocumentsAsync(tenantId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve archived documents for tenant {TenantId}", tenantId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task PermanentlyDeleteDocumentAsync(long documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogWarning("Permanently deleting document {DocumentId}", documentId);

            // Execute the deletion logic within an execution strategy to support retry on transient failures
            await _unitOfWork.ExecuteInTransactionAsync(async (ct) =>
            {
                var document = await _unitOfWork.Documents.GetByIdAsync(documentId, ct);
                if (document == null)
                {
                    throw new InvalidOperationException($"Document with ID {documentId} not found.");
                }

                // Delete from blob storage
                var blobDeleted = await _blobStorageService.DeleteAsync(
                    _blobStorageOptions.DefaultContainerName,
                    document.BlobPath,
                    ct);

                if (!blobDeleted)
                {
                    _logger.LogWarning(
                        "Blob {BlobPath} for document {DocumentId} was not found in storage",
                        document.BlobPath, documentId);
                }

                // Delete from database
                _unitOfWork.Documents.Remove(document);
                await _unitOfWork.SaveChangesAsync(ct);

                _logger.LogWarning("Successfully permanently deleted document {DocumentId}", documentId);

                return true; // Return a value to satisfy the generic return type
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to permanently delete document {DocumentId}", documentId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Document>> FindDuplicateDocumentsAsync(
        string contentHash,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(contentHash))
            throw new ArgumentException("Content hash cannot be null or empty.", nameof(contentHash));

        try
        {
            _logger.LogDebug("Finding duplicate documents with content hash {ContentHash}", contentHash);

            return await _unitOfWork.Documents.GetByContentHashAsync(contentHash, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find duplicate documents with content hash {ContentHash}", contentHash);
            throw;
        }
    }

    /// <summary>
    /// Computes the SHA256 hash of the content stream.
    /// </summary>
    /// <param name="content">The content stream.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The computed hash as a hexadecimal string.</returns>
    private static async Task<string> ComputeContentHashAsync(Stream content, CancellationToken cancellationToken)
    {
        var hash = await SHA256.HashDataAsync(content, cancellationToken);
        content.Position = 0; // Reset stream position
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// Generates a unique blob name for document storage.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="fileName">The original file name.</param>
    /// <param name="version">Optional version number.</param>
    /// <returns>A unique blob name.</returns>
    private static string GenerateBlobName(int tenantId, string fileName, int? version = null)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var guid = Guid.NewGuid().ToString("N")[..8]; // Use first 8 characters of GUID
        var extension = Path.GetExtension(fileName);
        var versionSuffix = version.HasValue ? $"_v{version.Value}" : string.Empty;

        return $"tenant-{tenantId}/{timestamp}_{guid}{versionSuffix}{extension}";
    }
}
