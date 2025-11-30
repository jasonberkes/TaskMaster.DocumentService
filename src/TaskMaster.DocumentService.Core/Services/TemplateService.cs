using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Interfaces;

namespace TaskMaster.DocumentService.Core.Services;

/// <summary>
/// Implementation of template management service with business logic for template operations and document generation with variable substitution.
/// Supports variable substitution in the format {{variableName}}.
/// </summary>
public class TemplateService : ITemplateService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IDocumentService _documentService;
    private readonly ILogger<TemplateService> _logger;
    private readonly BlobStorageOptions _blobStorageOptions;

    private static readonly Regex VariablePattern = new Regex(@"\{\{(\w+)\}\}", RegexOptions.Compiled);

    /// <summary>
    /// Initializes a new instance of the <see cref="TemplateService"/> class.
    /// </summary>
    /// <param name="unitOfWork">The unit of work for database operations.</param>
    /// <param name="blobStorageService">The blob storage service.</param>
    /// <param name="documentService">The document service for creating generated documents.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="blobStorageOptions">The blob storage configuration options.</param>
    public TemplateService(
        IUnitOfWork unitOfWork,
        IBlobStorageService blobStorageService,
        IDocumentService documentService,
        ILogger<TemplateService> logger,
        IOptions<BlobStorageOptions> blobStorageOptions)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _blobStorageService = blobStorageService ?? throw new ArgumentNullException(nameof(blobStorageService));
        _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _blobStorageOptions = blobStorageOptions?.Value ?? throw new ArgumentNullException(nameof(blobStorageOptions));
    }

    /// <inheritdoc/>
    public async Task<DocumentTemplate> CreateTemplateAsync(
        int tenantId,
        int documentTypeId,
        string name,
        string? description,
        Stream content,
        string fileName,
        string contentType,
        string? availableVariables,
        string? metadata,
        string? category,
        string createdBy,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Template name cannot be null or empty.", nameof(name));

        if (content == null)
            throw new ArgumentNullException(nameof(content));

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));

        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ArgumentException("Created by cannot be null or empty.", nameof(createdBy));

        try
        {
            _logger.LogInformation(
                "Creating template '{Name}' for tenant {TenantId} by user {CreatedBy}",
                name, tenantId, createdBy);

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

            var fileSize = content.Length;

            // Generate unique blob name for template
            var blobName = GenerateTemplateBlobName(tenantId, fileName);

            // Upload to blob storage
            var blobUri = await _blobStorageService.UploadAsync(
                _blobStorageOptions.DefaultContainerName,
                blobName,
                content,
                contentType,
                cancellationToken);

            // Create template entity
            var template = new DocumentTemplate
            {
                TenantId = tenantId,
                DocumentTypeId = documentTypeId,
                Name = name,
                Description = description,
                BlobPath = blobName,
                FileSizeBytes = fileSize,
                MimeType = contentType,
                OriginalFileName = fileName,
                AvailableVariables = availableVariables,
                Metadata = metadata,
                Category = category,
                Version = 1,
                IsCurrentVersion = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                IsDeleted = false
            };

            await _unitOfWork.DocumentTemplates.AddAsync(template, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully created template {TemplateId} with name '{Name}'",
                template.Id, name);

            return template;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create template '{Name}' for tenant {TenantId}", name, tenantId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<DocumentTemplate?> GetTemplateByIdAsync(long templateId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving template {TemplateId}", templateId);

            var template = await _unitOfWork.DocumentTemplates.GetByIdAsync(templateId, cancellationToken);

            if (template == null)
            {
                _logger.LogWarning("Template {TemplateId} not found", templateId);
            }

            return template;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve template {TemplateId}", templateId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DocumentTemplate>> GetTemplatesByTenantAsync(
        int tenantId,
        bool includeDeleted = false,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving templates for tenant {TenantId}", tenantId);

            return await _unitOfWork.DocumentTemplates.GetByTenantIdAsync(
                tenantId,
                includeDeleted,
                includeInactive,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve templates for tenant {TenantId}", tenantId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DocumentTemplate>> GetTemplatesByTypeAsync(
        int documentTypeId,
        bool includeDeleted = false,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving templates for document type {DocumentTypeId}", documentTypeId);

            return await _unitOfWork.DocumentTemplates.GetByDocumentTypeIdAsync(
                documentTypeId,
                includeDeleted,
                includeInactive,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve templates for document type {DocumentTypeId}", documentTypeId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DocumentTemplate>> GetTemplatesByCategoryAsync(
        int tenantId,
        string category,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving templates for tenant {TenantId} in category '{Category}'", tenantId, category);

            return await _unitOfWork.DocumentTemplates.GetByCategoryAsync(tenantId, category, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve templates for tenant {TenantId} in category '{Category}'", tenantId, category);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DocumentTemplate>> SearchTemplatesAsync(
        int tenantId,
        string searchTerm,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Searching templates for tenant {TenantId} with term '{SearchTerm}'", tenantId, searchTerm);

            return await _unitOfWork.DocumentTemplates.SearchByNameAsync(tenantId, searchTerm, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search templates for tenant {TenantId}", tenantId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<DocumentTemplate> UpdateTemplateMetadataAsync(
        long templateId,
        string? name,
        string? description,
        string? availableVariables,
        string? metadata,
        string? category,
        bool? isActive,
        string updatedBy,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(updatedBy))
            throw new ArgumentException("Updated by cannot be null or empty.", nameof(updatedBy));

        try
        {
            _logger.LogInformation("Updating metadata for template {TemplateId} by user {UpdatedBy}",
                templateId, updatedBy);

            var template = await _unitOfWork.DocumentTemplates.GetByIdAsync(templateId, cancellationToken);
            if (template == null)
            {
                throw new InvalidOperationException($"Template with ID {templateId} not found.");
            }

            if (template.IsDeleted)
            {
                throw new InvalidOperationException($"Cannot update metadata of deleted template {templateId}.");
            }

            // Update properties if provided
            if (!string.IsNullOrWhiteSpace(name))
            {
                template.Name = name;
            }

            if (description != null)
            {
                template.Description = description;
            }

            if (availableVariables != null)
            {
                template.AvailableVariables = availableVariables;
            }

            if (metadata != null)
            {
                template.Metadata = metadata;
            }

            if (category != null)
            {
                template.Category = category;
            }

            if (isActive.HasValue)
            {
                template.IsActive = isActive.Value;
            }

            template.UpdatedAt = DateTime.UtcNow;
            template.UpdatedBy = updatedBy;

            _unitOfWork.DocumentTemplates.Update(template);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully updated metadata for template {TemplateId}", templateId);

            return template;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update metadata for template {TemplateId}", templateId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Stream> DownloadTemplateAsync(long templateId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Downloading template {TemplateId}", templateId);

            var template = await _unitOfWork.DocumentTemplates.GetByIdAsync(templateId, cancellationToken);
            if (template == null)
            {
                throw new InvalidOperationException($"Template with ID {templateId} not found.");
            }

            if (template.IsDeleted)
            {
                throw new InvalidOperationException($"Cannot download deleted template {templateId}.");
            }

            var stream = await _blobStorageService.DownloadAsync(
                _blobStorageOptions.DefaultContainerName,
                template.BlobPath,
                cancellationToken);

            _logger.LogInformation("Successfully downloaded template {TemplateId}", templateId);

            return stream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download template {TemplateId}", templateId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Document> GenerateDocumentFromTemplateAsync(
        long templateId,
        string title,
        string? description,
        Dictionary<string, string> variables,
        string? metadata,
        string? tags,
        string createdBy,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Document title cannot be null or empty.", nameof(title));

        if (variables == null)
            throw new ArgumentNullException(nameof(variables));

        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ArgumentException("Created by cannot be null or empty.", nameof(createdBy));

        try
        {
            _logger.LogInformation(
                "Generating document from template {TemplateId} with title '{Title}' by user {CreatedBy}",
                templateId, title, createdBy);

            // Get the template
            var template = await _unitOfWork.DocumentTemplates.GetByIdAsync(templateId, cancellationToken);
            if (template == null)
            {
                throw new InvalidOperationException($"Template with ID {templateId} not found.");
            }

            if (template.IsDeleted)
            {
                throw new InvalidOperationException($"Cannot generate document from deleted template {templateId}.");
            }

            if (!template.IsActive)
            {
                throw new InvalidOperationException($"Cannot generate document from inactive template {templateId}.");
            }

            // Download template content
            var templateStream = await _blobStorageService.DownloadAsync(
                _blobStorageOptions.DefaultContainerName,
                template.BlobPath,
                cancellationToken);

            // Perform variable substitution
            var processedStream = await PerformVariableSubstitutionAsync(
                templateStream,
                variables,
                template.MimeType,
                cancellationToken);

            // Generate a unique filename for the generated document
            var generatedFileName = GenerateDocumentFileName(template.OriginalFileName ?? "document", title);

            // Create the document using the document service
            var document = await _documentService.CreateDocumentAsync(
                template.TenantId,
                template.DocumentTypeId,
                title,
                description,
                processedStream,
                generatedFileName,
                template.MimeType,
                metadata,
                tags,
                createdBy,
                cancellationToken);

            _logger.LogInformation(
                "Successfully generated document {DocumentId} from template {TemplateId}",
                document.Id, templateId);

            return document;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate document from template {TemplateId}", templateId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task DeleteTemplateAsync(
        long templateId,
        string deletedBy,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deletedBy))
            throw new ArgumentException("Deleted by cannot be null or empty.", nameof(deletedBy));

        try
        {
            _logger.LogInformation(
                "Soft deleting template {TemplateId} by user {DeletedBy}",
                templateId, deletedBy);

            await _unitOfWork.DocumentTemplates.SoftDeleteAsync(templateId, deletedBy, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully soft deleted template {TemplateId}", templateId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to soft delete template {TemplateId}", templateId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task RestoreTemplateAsync(long templateId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Restoring template {TemplateId}", templateId);

            await _unitOfWork.DocumentTemplates.RestoreAsync(templateId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully restored template {TemplateId}", templateId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore template {TemplateId}", templateId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task PermanentlyDeleteTemplateAsync(long templateId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogWarning("Permanently deleting template {TemplateId}", templateId);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var template = await _unitOfWork.DocumentTemplates.GetByIdAsync(templateId, cancellationToken);
            if (template == null)
            {
                throw new InvalidOperationException($"Template with ID {templateId} not found.");
            }

            // Delete from blob storage
            var blobDeleted = await _blobStorageService.DeleteAsync(
                _blobStorageOptions.DefaultContainerName,
                template.BlobPath,
                cancellationToken);

            if (!blobDeleted)
            {
                _logger.LogWarning(
                    "Blob {BlobPath} for template {TemplateId} was not found in storage",
                    template.BlobPath, templateId);
            }

            // Delete from database
            _unitOfWork.DocumentTemplates.Remove(template);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogWarning("Successfully permanently deleted template {TemplateId}", templateId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to permanently delete template {TemplateId}", templateId);
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Performs variable substitution on template content.
    /// Replaces placeholders in the format {{variableName}} with provided values.
    /// </summary>
    /// <param name="templateStream">The template content stream.</param>
    /// <param name="variables">Dictionary of variable names and their values.</param>
    /// <param name="mimeType">The MIME type of the template.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A new stream with substituted content.</returns>
    private static async Task<Stream> PerformVariableSubstitutionAsync(
        Stream templateStream,
        Dictionary<string, string> variables,
        string mimeType,
        CancellationToken cancellationToken)
    {
        // For text-based formats, perform substitution
        if (IsTextBasedFormat(mimeType))
        {
            using var reader = new StreamReader(templateStream, Encoding.UTF8);
            var content = await reader.ReadToEndAsync(cancellationToken);

            // Replace each variable placeholder with its value
            var processedContent = VariablePattern.Replace(content, match =>
            {
                var variableName = match.Groups[1].Value;
                return variables.TryGetValue(variableName, out var value) ? value : match.Value;
            });

            // Convert back to stream
            var bytes = Encoding.UTF8.GetBytes(processedContent);
            return new MemoryStream(bytes);
        }

        // For binary formats (e.g., DOCX, PDF), return original stream
        // In a production system, you might use libraries like DocumentFormat.OpenXml
        // for DOCX or iTextSharp for PDF to perform substitutions
        templateStream.Position = 0;
        var memoryStream = new MemoryStream();
        await templateStream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;
        return memoryStream;
    }

    /// <summary>
    /// Checks if a MIME type represents a text-based format suitable for simple text substitution.
    /// </summary>
    /// <param name="mimeType">The MIME type to check.</param>
    /// <returns>True if the format is text-based; otherwise false.</returns>
    private static bool IsTextBasedFormat(string mimeType)
    {
        return mimeType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) ||
               mimeType.Contains("json", StringComparison.OrdinalIgnoreCase) ||
               mimeType.Contains("xml", StringComparison.OrdinalIgnoreCase) ||
               mimeType.Contains("html", StringComparison.OrdinalIgnoreCase) ||
               mimeType.Contains("markdown", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Generates a unique blob name for template storage.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="fileName">The original file name.</param>
    /// <returns>A unique blob name.</returns>
    private static string GenerateTemplateBlobName(int tenantId, string fileName)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var guid = Guid.NewGuid().ToString("N")[..8];
        var extension = Path.GetExtension(fileName);

        return $"tenant-{tenantId}/templates/{timestamp}_{guid}{extension}";
    }

    /// <summary>
    /// Generates a filename for a document generated from a template.
    /// </summary>
    /// <param name="templateFileName">The template's original filename.</param>
    /// <param name="documentTitle">The document title.</param>
    /// <returns>A generated filename.</returns>
    private static string GenerateDocumentFileName(string templateFileName, string documentTitle)
    {
        var extension = Path.GetExtension(templateFileName);
        var sanitizedTitle = SanitizeFileName(documentTitle);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

        return $"{sanitizedTitle}_{timestamp}{extension}";
    }

    /// <summary>
    /// Sanitizes a string to be used as a filename.
    /// </summary>
    /// <param name="fileName">The filename to sanitize.</param>
    /// <returns>A sanitized filename.</returns>
    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        return sanitized.Length > 50 ? sanitized[..50] : sanitized;
    }
}
