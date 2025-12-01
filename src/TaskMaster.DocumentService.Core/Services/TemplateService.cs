using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Interfaces;

namespace TaskMaster.DocumentService.Core.Services;

/// <summary>
/// Implementation of template service for document template management and document generation.
/// </summary>
public class TemplateService : ITemplateService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBlobStorageService _blobStorageService;
    private readonly ILogger<TemplateService> _logger;
    private readonly BlobStorageOptions _blobStorageOptions;

    // Regex pattern for matching template variables: {{variableName}}
    private static readonly Regex VariablePattern = new(@"\{\{(\w+)\}\}", RegexOptions.Compiled);

    /// <summary>
    /// Initializes a new instance of the <see cref="TemplateService"/> class.
    /// </summary>
    /// <param name="unitOfWork">The unit of work for database operations.</param>
    /// <param name="blobStorageService">The blob storage service.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="blobStorageOptions">The blob storage configuration options.</param>
    public TemplateService(
        IUnitOfWork unitOfWork,
        IBlobStorageService blobStorageService,
        ILogger<TemplateService> logger,
        IOptions<BlobStorageOptions> blobStorageOptions)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _blobStorageService = blobStorageService ?? throw new ArgumentNullException(nameof(blobStorageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _blobStorageOptions = blobStorageOptions?.Value ?? throw new ArgumentNullException(nameof(blobStorageOptions));
    }

    /// <inheritdoc/>
    public async Task<DocumentTemplate> CreateTemplateAsync(
        int tenantId,
        int documentTypeId,
        string name,
        string description,
        string content,
        string mimeType,
        string? fileExtension,
        IEnumerable<TemplateVariable> variables,
        string? defaultTitlePattern,
        string? defaultDescriptionPattern,
        string? metadata,
        string? tags,
        string createdBy,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Template name cannot be null or empty.", nameof(name));

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Template content cannot be null or empty.", nameof(content));

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

            // Check if template with same name exists for this tenant
            var existingTemplate = await _unitOfWork.Templates.GetByTenantAndNameAsync(tenantId, name, cancellationToken);
            if (existingTemplate != null)
            {
                throw new InvalidOperationException($"Template with name '{name}' already exists for tenant {tenantId}.");
            }

            // Create template entity
            var template = new DocumentTemplate
            {
                TenantId = tenantId,
                DocumentTypeId = documentTypeId,
                Name = name,
                Description = description,
                Content = content,
                MimeType = mimeType,
                FileExtension = fileExtension,
                DefaultTitlePattern = defaultTitlePattern,
                DefaultDescriptionPattern = defaultDescriptionPattern,
                Metadata = metadata,
                Tags = tags,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                IsDeleted = false
            };

            await _unitOfWork.Templates.AddAsync(template, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Add variables
            if (variables?.Any() == true)
            {
                var sortOrder = 0;
                foreach (var variable in variables)
                {
                    variable.TemplateId = template.Id;
                    variable.SortOrder = sortOrder++;
                    variable.CreatedAt = DateTime.UtcNow;
                }

                // Clear the collection and re-add to ensure EF tracks properly
                template.Variables.Clear();
                foreach (var variable in variables)
                {
                    template.Variables.Add(variable);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation(
                "Successfully created template {TemplateId} with name '{Name}'",
                template.Id, name);

            return await _unitOfWork.Templates.GetTemplateWithVariablesAsync(template.Id, cancellationToken)
                ?? template;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create template '{Name}' for tenant {TenantId}", name, tenantId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<DocumentTemplate?> GetTemplateByIdAsync(int templateId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving template {TemplateId}", templateId);

            return await _unitOfWork.Templates.GetTemplateWithVariablesAsync(templateId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve template {TemplateId}", templateId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<DocumentTemplate?> GetTemplateByNameAsync(int tenantId, string templateName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving template '{TemplateName}' for tenant {TenantId}", templateName, tenantId);

            return await _unitOfWork.Templates.GetByTenantAndNameAsync(tenantId, templateName, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve template '{TemplateName}' for tenant {TenantId}", templateName, tenantId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DocumentTemplate>> GetTemplatesByTenantAsync(int tenantId, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug(
                "Retrieving templates for tenant {TenantId}, includeDeleted: {IncludeDeleted}",
                tenantId, includeDeleted);

            return await _unitOfWork.Templates.GetByTenantIdAsync(tenantId, includeDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve templates for tenant {TenantId}", tenantId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DocumentTemplate>> GetActiveTemplatesAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving active templates for tenant {TenantId}", tenantId);

            return await _unitOfWork.Templates.GetActiveTemplatesAsync(tenantId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve active templates for tenant {TenantId}", tenantId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<DocumentTemplate> UpdateTemplateAsync(
        int templateId,
        string? name,
        string? description,
        string? content,
        string? mimeType,
        string? fileExtension,
        IEnumerable<TemplateVariable>? variables,
        string? defaultTitlePattern,
        string? defaultDescriptionPattern,
        string? metadata,
        string? tags,
        bool? isActive,
        string updatedBy,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(updatedBy))
            throw new ArgumentException("Updated by cannot be null or empty.", nameof(updatedBy));

        try
        {
            _logger.LogInformation("Updating template {TemplateId} by user {UpdatedBy}", templateId, updatedBy);

            var template = await _unitOfWork.Templates.GetTemplateWithVariablesAsync(templateId, cancellationToken);
            if (template == null)
            {
                throw new InvalidOperationException($"Template with ID {templateId} not found.");
            }

            if (template.IsDeleted)
            {
                throw new InvalidOperationException($"Cannot update deleted template {templateId}.");
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

            if (!string.IsNullOrWhiteSpace(content))
            {
                template.Content = content;
            }

            if (!string.IsNullOrWhiteSpace(mimeType))
            {
                template.MimeType = mimeType;
            }

            if (fileExtension != null)
            {
                template.FileExtension = fileExtension;
            }

            if (defaultTitlePattern != null)
            {
                template.DefaultTitlePattern = defaultTitlePattern;
            }

            if (defaultDescriptionPattern != null)
            {
                template.DefaultDescriptionPattern = defaultDescriptionPattern;
            }

            if (metadata != null)
            {
                template.Metadata = metadata;
            }

            if (tags != null)
            {
                template.Tags = tags;
            }

            if (isActive.HasValue)
            {
                template.IsActive = isActive.Value;
            }

            template.UpdatedAt = DateTime.UtcNow;
            template.UpdatedBy = updatedBy;

            // Update variables if provided
            if (variables != null)
            {
                // Remove existing variables
                var existingVariables = template.Variables.ToList();
                foreach (var existingVar in existingVariables)
                {
                    template.Variables.Remove(existingVar);
                }

                // Add new variables
                var sortOrder = 0;
                foreach (var variable in variables)
                {
                    variable.TemplateId = templateId;
                    variable.SortOrder = sortOrder++;
                    variable.CreatedAt = DateTime.UtcNow;
                    template.Variables.Add(variable);
                }
            }

            _unitOfWork.Templates.Update(template);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully updated template {TemplateId}", templateId);

            return await _unitOfWork.Templates.GetTemplateWithVariablesAsync(templateId, cancellationToken)
                ?? template;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update template {TemplateId}", templateId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task DeleteTemplateAsync(int templateId, string deletedBy, string? deletedReason = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deletedBy))
            throw new ArgumentException("Deleted by cannot be null or empty.", nameof(deletedBy));

        try
        {
            _logger.LogInformation(
                "Soft deleting template {TemplateId} by user {DeletedBy}",
                templateId, deletedBy);

            await _unitOfWork.Templates.SoftDeleteAsync(templateId, deletedBy, deletedReason, cancellationToken);
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
    public async Task RestoreTemplateAsync(int templateId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Restoring template {TemplateId}", templateId);

            await _unitOfWork.Templates.RestoreAsync(templateId, cancellationToken);
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
    public async Task<Document> CreateDocumentFromTemplateAsync(
        int templateId,
        Dictionary<string, string> variables,
        string? title,
        string? description,
        string? metadata,
        string? tags,
        string createdBy,
        CancellationToken cancellationToken = default)
    {
        if (variables == null)
            throw new ArgumentNullException(nameof(variables));

        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ArgumentException("Created by cannot be null or empty.", nameof(createdBy));

        try
        {
            _logger.LogInformation(
                "Creating document from template {TemplateId} by user {CreatedBy}",
                templateId, createdBy);

            // Get template with variables
            var template = await _unitOfWork.Templates.GetTemplateWithVariablesAsync(templateId, cancellationToken);
            if (template == null)
            {
                throw new InvalidOperationException($"Template with ID {templateId} not found.");
            }

            if (!template.IsActive)
            {
                throw new InvalidOperationException($"Template {templateId} is not active.");
            }

            if (template.IsDeleted)
            {
                throw new InvalidOperationException($"Template {templateId} is deleted.");
            }

            // Validate variables
            var validationErrors = ValidateVariables(template, variables);
            if (validationErrors.Any())
            {
                var errorMessages = string.Join("; ", validationErrors.Select(e => $"{e.Key}: {e.Value}"));
                throw new ArgumentException($"Variable validation failed: {errorMessages}", nameof(variables));
            }

            // Substitute variables in content
            var processedContent = SubstituteVariables(template.Content, variables);

            // Generate title from pattern or use provided title
            var documentTitle = title;
            if (string.IsNullOrWhiteSpace(documentTitle) && !string.IsNullOrWhiteSpace(template.DefaultTitlePattern))
            {
                documentTitle = SubstituteVariables(template.DefaultTitlePattern, variables);
            }

            if (string.IsNullOrWhiteSpace(documentTitle))
            {
                documentTitle = $"{template.Name} - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
            }

            // Generate description from pattern or use provided description
            var documentDescription = description;
            if (string.IsNullOrWhiteSpace(documentDescription) && !string.IsNullOrWhiteSpace(template.DefaultDescriptionPattern))
            {
                documentDescription = SubstituteVariables(template.DefaultDescriptionPattern, variables);
            }

            // Convert content to stream
            var contentBytes = Encoding.UTF8.GetBytes(processedContent);
            using var contentStream = new MemoryStream(contentBytes);

            // Generate file name
            var fileExtension = template.FileExtension ?? ".txt";
            if (!fileExtension.StartsWith("."))
            {
                fileExtension = "." + fileExtension;
            }
            var fileName = $"{template.Name}_{DateTime.UtcNow:yyyyMMddHHmmss}{fileExtension}";

            // Calculate content hash
            var contentHash = await ComputeContentHashAsync(contentStream, cancellationToken);
            contentStream.Position = 0;

            // Generate unique blob name
            var blobName = GenerateBlobName(template.TenantId, fileName);

            // Upload to blob storage
            await _blobStorageService.UploadAsync(
                _blobStorageOptions.DefaultContainerName,
                blobName,
                contentStream,
                template.MimeType,
                cancellationToken);

            // Create document entity
            var document = new Document
            {
                TenantId = template.TenantId,
                DocumentTypeId = template.DocumentTypeId,
                Title = documentTitle,
                Description = documentDescription,
                BlobPath = blobName,
                ContentHash = contentHash,
                FileSizeBytes = contentBytes.Length,
                MimeType = template.MimeType,
                OriginalFileName = fileName,
                Metadata = metadata ?? template.Metadata,
                Tags = tags ?? template.Tags,
                Version = 1,
                IsCurrentVersion = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                IsDeleted = false,
                IsArchived = false
            };

            await _unitOfWork.Documents.AddAsync(document, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully created document {DocumentId} from template {TemplateId}",
                document.Id, templateId);

            return document;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create document from template {TemplateId}", templateId);
            throw;
        }
    }

    /// <inheritdoc/>
    public Dictionary<string, string> ValidateVariables(DocumentTemplate template, Dictionary<string, string> variables)
    {
        var errors = new Dictionary<string, string>();

        if (template.Variables == null || !template.Variables.Any())
        {
            return errors;
        }

        foreach (var variableDefinition in template.Variables)
        {
            var hasValue = variables.TryGetValue(variableDefinition.Name, out var value);

            // Check required variables
            if (variableDefinition.IsRequired && (!hasValue || string.IsNullOrWhiteSpace(value)))
            {
                errors[variableDefinition.Name] = $"Variable '{variableDefinition.DisplayName}' is required.";
                continue;
            }

            // Skip validation if no value provided and not required
            if (!hasValue || string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            // Validate against regex pattern if provided
            if (!string.IsNullOrWhiteSpace(variableDefinition.ValidationPattern))
            {
                try
                {
                    var regex = new Regex(variableDefinition.ValidationPattern);
                    if (!regex.IsMatch(value))
                    {
                        var message = !string.IsNullOrWhiteSpace(variableDefinition.ValidationMessage)
                            ? variableDefinition.ValidationMessage
                            : $"Variable '{variableDefinition.DisplayName}' does not match the required pattern.";
                        errors[variableDefinition.Name] = message;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Invalid regex pattern for variable {VariableName}", variableDefinition.Name);
                }
            }

            // Data type validation
            switch (variableDefinition.DataType.ToLowerInvariant())
            {
                case "number":
                case "integer":
                    if (!int.TryParse(value, out _) && !double.TryParse(value, out _))
                    {
                        errors[variableDefinition.Name] = $"Variable '{variableDefinition.DisplayName}' must be a number.";
                    }
                    break;

                case "date":
                case "datetime":
                    if (!DateTime.TryParse(value, out _))
                    {
                        errors[variableDefinition.Name] = $"Variable '{variableDefinition.DisplayName}' must be a valid date.";
                    }
                    break;

                case "boolean":
                case "bool":
                    if (!bool.TryParse(value, out _))
                    {
                        errors[variableDefinition.Name] = $"Variable '{variableDefinition.DisplayName}' must be true or false.";
                    }
                    break;
            }
        }

        return errors;
    }

    /// <inheritdoc/>
    public string SubstituteVariables(string content, Dictionary<string, string> variables)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return content;
        }

        return VariablePattern.Replace(content, match =>
        {
            var variableName = match.Groups[1].Value;

            // Return the value if found, otherwise return the original placeholder
            if (variables.TryGetValue(variableName, out var value))
            {
                return value ?? string.Empty;
            }

            // Log warning for undefined variables
            _logger.LogWarning("Variable '{VariableName}' not found in provided values", variableName);
            return match.Value; // Keep the original placeholder
        });
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
        content.Position = 0;
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// Generates a unique blob name for document storage.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="fileName">The original file name.</param>
    /// <returns>A unique blob name.</returns>
    private static string GenerateBlobName(int tenantId, string fileName)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var guid = Guid.NewGuid().ToString("N")[..8];
        var extension = Path.GetExtension(fileName);

        return $"tenant-{tenantId}/{timestamp}_{guid}{extension}";
    }
}
