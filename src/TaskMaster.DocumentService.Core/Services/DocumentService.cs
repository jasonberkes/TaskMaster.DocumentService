using Microsoft.Extensions.Logging;
using TaskMaster.DocumentService.Core.DTOs;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Interfaces;

namespace TaskMaster.DocumentService.Core.Services;

/// <summary>
/// Service implementation for document business logic operations
/// </summary>
public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _repository;
    private readonly ILogger<DocumentService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentService"/> class
    /// </summary>
    /// <param name="repository">The document repository</param>
    /// <param name="logger">The logger instance</param>
    public DocumentService(IDocumentRepository repository, ILogger<DocumentService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<DocumentDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving document with ID {DocumentId}", id);

            var document = await _repository.GetByIdAsync(id, false, cancellationToken);
            if (document == null)
            {
                _logger.LogWarning("Document with ID {DocumentId} not found", id);
                return null;
            }

            return MapToDto(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document with ID {DocumentId}", id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DocumentDto>> GetByTenantIdAsync(int tenantId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving documents for tenant {TenantId} (skip: {Skip}, take: {Take})", tenantId, skip, take);

            var documents = await _repository.GetByTenantIdAsync(tenantId, skip, take, false, cancellationToken);
            return documents.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving documents for tenant {TenantId}", tenantId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DocumentDto>> GetVersionsAsync(long documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving versions for document {DocumentId}", documentId);

            var versions = await _repository.GetVersionsAsync(documentId, cancellationToken);
            return versions.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving versions for document {DocumentId}", documentId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<DocumentDto> CreateAsync(CreateDocumentDto createDto, CancellationToken cancellationToken = default)
    {
        try
        {
            if (createDto == null)
            {
                throw new ArgumentNullException(nameof(createDto));
            }

            _logger.LogInformation("Creating new document for tenant {TenantId}", createDto.TenantId);

            // Check for duplicate content hash if provided
            if (!string.IsNullOrEmpty(createDto.ContentHash))
            {
                var isDuplicate = await _repository.ExistsByContentHashAsync(createDto.TenantId, createDto.ContentHash, cancellationToken);
                if (isDuplicate)
                {
                    _logger.LogWarning("Document with content hash {ContentHash} already exists for tenant {TenantId}",
                        createDto.ContentHash, createDto.TenantId);
                    throw new InvalidOperationException($"A document with the same content already exists (hash: {createDto.ContentHash})");
                }
            }

            var document = new Document
            {
                TenantId = createDto.TenantId,
                DocumentTypeId = createDto.DocumentTypeId,
                Title = createDto.Title,
                Description = createDto.Description,
                BlobPath = createDto.BlobPath,
                ContentHash = createDto.ContentHash,
                FileSizeBytes = createDto.FileSizeBytes,
                MimeType = createDto.MimeType,
                OriginalFileName = createDto.OriginalFileName,
                Metadata = createDto.Metadata,
                Tags = createDto.Tags,
                ExtractedText = createDto.ExtractedText,
                CreatedBy = createDto.CreatedBy,
                Version = 1,
                IsCurrentVersion = true,
                ParentDocumentId = null
            };

            var created = await _repository.CreateAsync(document, cancellationToken);

            _logger.LogInformation("Successfully created document with ID {DocumentId}", created.Id);

            return MapToDto(created);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Error creating document for tenant {TenantId}", createDto?.TenantId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<DocumentDto> CreateVersionAsync(long documentId, CreateDocumentVersionDto versionDto, CancellationToken cancellationToken = default)
    {
        try
        {
            if (versionDto == null)
            {
                throw new ArgumentNullException(nameof(versionDto));
            }

            _logger.LogInformation("Creating new version for document {DocumentId}", documentId);

            // Get the original document
            var originalDocument = await _repository.GetByIdAsync(documentId, false, cancellationToken);
            if (originalDocument == null)
            {
                _logger.LogWarning("Original document with ID {DocumentId} not found", documentId);
                throw new InvalidOperationException($"Document with ID {documentId} not found");
            }

            // Determine the parent document ID (for versioning chain)
            var parentDocId = originalDocument.ParentDocumentId ?? originalDocument.Id;

            // Get current version to determine next version number
            var currentVersion = await _repository.GetCurrentVersionAsync(parentDocId, cancellationToken);
            var nextVersionNumber = (currentVersion?.Version ?? 0) + 1;

            // Mark current version as not current
            if (currentVersion != null && currentVersion.IsCurrentVersion)
            {
                currentVersion.IsCurrentVersion = false;
                await _repository.UpdateAsync(currentVersion, cancellationToken);
            }

            // Create new version
            var newVersion = new Document
            {
                TenantId = originalDocument.TenantId,
                DocumentTypeId = originalDocument.DocumentTypeId,
                Title = originalDocument.Title,
                Description = originalDocument.Description,
                BlobPath = versionDto.BlobPath,
                ContentHash = versionDto.ContentHash,
                FileSizeBytes = versionDto.FileSizeBytes,
                MimeType = versionDto.MimeType,
                OriginalFileName = versionDto.OriginalFileName,
                Metadata = originalDocument.Metadata,
                Tags = originalDocument.Tags,
                ExtractedText = versionDto.ExtractedText,
                CreatedBy = versionDto.CreatedBy,
                Version = nextVersionNumber,
                IsCurrentVersion = true,
                ParentDocumentId = parentDocId
            };

            var created = await _repository.CreateAsync(newVersion, cancellationToken);

            _logger.LogInformation("Successfully created version {Version} for document {DocumentId} with new ID {NewDocumentId}",
                nextVersionNumber, documentId, created.Id);

            return MapToDto(created);
        }
        catch (Exception ex) when (ex is not InvalidOperationException and not ArgumentNullException)
        {
            _logger.LogError(ex, "Error creating version for document {DocumentId}", documentId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<DocumentDto?> UpdateAsync(long id, UpdateDocumentDto updateDto, CancellationToken cancellationToken = default)
    {
        try
        {
            if (updateDto == null)
            {
                throw new ArgumentNullException(nameof(updateDto));
            }

            _logger.LogInformation("Updating document {DocumentId}", id);

            var document = await _repository.GetByIdAsync(id, false, cancellationToken);
            if (document == null)
            {
                _logger.LogWarning("Document with ID {DocumentId} not found for update", id);
                return null;
            }

            // Update only provided fields
            if (updateDto.Title != null)
            {
                document.Title = updateDto.Title;
            }

            if (updateDto.Description != null)
            {
                document.Description = updateDto.Description;
            }

            if (updateDto.Metadata != null)
            {
                document.Metadata = updateDto.Metadata;
            }

            if (updateDto.Tags != null)
            {
                document.Tags = updateDto.Tags;
            }

            document.UpdatedBy = updateDto.UpdatedBy;

            var updated = await _repository.UpdateAsync(document, cancellationToken);

            _logger.LogInformation("Successfully updated document {DocumentId}", id);

            return MapToDto(updated);
        }
        catch (Exception ex) when (ex is not ArgumentNullException)
        {
            _logger.LogError(ex, "Error updating document {DocumentId}", id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(long id, string? deletedBy, string? deletedReason, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Soft deleting document {DocumentId}", id);

            var result = await _repository.SoftDeleteAsync(id, deletedBy, deletedReason, cancellationToken);

            if (result)
            {
                _logger.LogInformation("Successfully soft deleted document {DocumentId}", id);
            }
            else
            {
                _logger.LogWarning("Document {DocumentId} not found for deletion", id);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {DocumentId}", id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ArchiveAsync(long id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Archiving document {DocumentId}", id);

            var result = await _repository.ArchiveAsync(id, cancellationToken);

            if (result)
            {
                _logger.LogInformation("Successfully archived document {DocumentId}", id);
            }
            else
            {
                _logger.LogWarning("Document {DocumentId} not found for archiving", id);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving document {DocumentId}", id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> UnarchiveAsync(long id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Unarchiving document {DocumentId}", id);

            var result = await _repository.UnarchiveAsync(id, cancellationToken);

            if (result)
            {
                _logger.LogInformation("Successfully unarchived document {DocumentId}", id);
            }
            else
            {
                _logger.LogWarning("Document {DocumentId} not found for unarchiving", id);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unarchiving document {DocumentId}", id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<int> GetCountByTenantIdAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting document count for tenant {TenantId}", tenantId);

            return await _repository.GetCountByTenantIdAsync(tenantId, false, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document count for tenant {TenantId}", tenantId);
            throw;
        }
    }

    /// <summary>
    /// Maps a Document entity to a DocumentDto
    /// </summary>
    /// <param name="document">The document entity</param>
    /// <returns>The document DTO</returns>
    private static DocumentDto MapToDto(Document document)
    {
        return new DocumentDto
        {
            Id = document.Id,
            TenantId = document.TenantId,
            DocumentTypeId = document.DocumentTypeId,
            Title = document.Title,
            Description = document.Description,
            BlobPath = document.BlobPath,
            ContentHash = document.ContentHash,
            FileSizeBytes = document.FileSizeBytes,
            MimeType = document.MimeType,
            OriginalFileName = document.OriginalFileName,
            Metadata = document.Metadata,
            Tags = document.Tags,
            Version = document.Version,
            ParentDocumentId = document.ParentDocumentId,
            IsCurrentVersion = document.IsCurrentVersion,
            IsArchived = document.IsArchived,
            CreatedAt = document.CreatedAt,
            CreatedBy = document.CreatedBy,
            UpdatedAt = document.UpdatedAt,
            UpdatedBy = document.UpdatedBy
        };
    }
}
