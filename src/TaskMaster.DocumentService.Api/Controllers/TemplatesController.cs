using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskMaster.DocumentService.Api.Authorization;
using TaskMaster.DocumentService.Api.Extensions;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Interfaces;

namespace TaskMaster.DocumentService.Api.Controllers;

/// <summary>
/// Controller for document template management and document generation from templates.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TemplatesController : ControllerBase
{
    private readonly ITemplateService _templateService;
    private readonly ILogger<TemplatesController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TemplatesController"/> class.
    /// </summary>
    /// <param name="templateService">The template service.</param>
    /// <param name="logger">The logger.</param>
    public TemplatesController(
        ITemplateService templateService,
        ILogger<TemplatesController> logger)
    {
        _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new document template.
    /// </summary>
    /// <param name="request">The create template request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created template.</returns>
    /// <response code="201">Returns the newly created template.</response>
    /// <response code="400">If the request is invalid.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user does not have access to the specified tenant.</response>
    [HttpPost]
    [TenantAuthorization("tenantId")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateTemplate(
        [FromBody] CreateTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { error = "InvalidName", message = "Template name is required." });
            }

            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest(new { error = "InvalidContent", message = "Template content is required." });
            }

            var createdBy = User.Identity?.Name ?? "system";

            _logger.LogInformation(
                "Creating template '{Name}' for tenant {TenantId} by user {CreatedBy}",
                request.Name, request.TenantId, createdBy);

            var variables = request.Variables?.Select(v => new TemplateVariable
            {
                Name = v.Name,
                DisplayName = v.DisplayName,
                Description = v.Description,
                DataType = v.DataType ?? "string",
                DefaultValue = v.DefaultValue,
                IsRequired = v.IsRequired,
                ValidationPattern = v.ValidationPattern,
                ValidationMessage = v.ValidationMessage
            }) ?? Enumerable.Empty<TemplateVariable>();

            var template = await _templateService.CreateTemplateAsync(
                request.TenantId,
                request.DocumentTypeId,
                request.Name,
                request.Description ?? string.Empty,
                request.Content,
                request.MimeType ?? "text/plain",
                request.FileExtension,
                variables,
                request.DefaultTitlePattern,
                request.DefaultDescriptionPattern,
                request.Metadata,
                request.Tags,
                createdBy,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetTemplateById),
                new { templateId = template.Id },
                MapTemplateToResponse(template));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when creating template");
            return BadRequest(new { error = "InvalidOperation", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create template");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while creating the template." });
        }
    }

    /// <summary>
    /// Gets a template by its identifier.
    /// </summary>
    /// <param name="templateId">The template identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The template information.</returns>
    /// <response code="200">Returns the template information.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the template is not found.</response>
    [HttpGet("{templateId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTemplateById(int templateId, CancellationToken cancellationToken = default)
    {
        try
        {
            var template = await _templateService.GetTemplateByIdAsync(templateId, cancellationToken);

            if (template == null)
            {
                _logger.LogWarning("Template {TemplateId} not found", templateId);
                return NotFound(new { error = "TemplateNotFound", message = $"Template with ID {templateId} not found." });
            }

            // Verify tenant access
            var userTenantId = User.GetTenantId();
            if (template.TenantId != userTenantId)
            {
                _logger.LogWarning(
                    "User from tenant {UserTenantId} attempted to access template from tenant {TemplateTenantId}",
                    userTenantId, template.TenantId);
                return NotFound(new { error = "TemplateNotFound", message = $"Template with ID {templateId} not found." });
            }

            return Ok(MapTemplateToResponse(template));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve template {TemplateId}", templateId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while retrieving the template." });
        }
    }

    /// <summary>
    /// Gets all templates for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="includeDeleted">Whether to include soft-deleted templates.</param>
    /// <param name="activeOnly">Whether to return only active templates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of templates.</returns>
    /// <response code="200">Returns the list of templates.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user does not have access to the specified tenant.</response>
    [HttpGet("tenant/{tenantId}")]
    [TenantAuthorization("tenantId")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTemplatesByTenant(
        int tenantId,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] bool activeOnly = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Retrieving templates for tenant {TenantId}, includeDeleted: {IncludeDeleted}, activeOnly: {ActiveOnly}",
                tenantId, includeDeleted, activeOnly);

            var templates = activeOnly
                ? await _templateService.GetActiveTemplatesAsync(tenantId, cancellationToken)
                : await _templateService.GetTemplatesByTenantAsync(tenantId, includeDeleted, cancellationToken);

            return Ok(templates.Select(MapTemplateToListResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve templates for tenant {TenantId}", tenantId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while retrieving templates." });
        }
    }

    /// <summary>
    /// Updates a template.
    /// </summary>
    /// <param name="templateId">The template identifier.</param>
    /// <param name="request">The update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated template.</returns>
    /// <response code="200">Returns the updated template.</response>
    /// <response code="400">If the request is invalid.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the template is not found.</response>
    [HttpPut("{templateId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTemplate(
        int templateId,
        [FromBody] UpdateTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify the template exists and belongs to user's tenant
            var existingTemplate = await _templateService.GetTemplateByIdAsync(templateId, cancellationToken);
            if (existingTemplate == null)
            {
                return NotFound(new { error = "TemplateNotFound", message = $"Template with ID {templateId} not found." });
            }

            var userTenantId = User.GetTenantId();
            if (existingTemplate.TenantId != userTenantId)
            {
                return NotFound(new { error = "TemplateNotFound", message = $"Template with ID {templateId} not found." });
            }

            var updatedBy = User.Identity?.Name ?? "system";

            var variables = request.Variables?.Select(v => new TemplateVariable
            {
                Name = v.Name,
                DisplayName = v.DisplayName,
                Description = v.Description,
                DataType = v.DataType ?? "string",
                DefaultValue = v.DefaultValue,
                IsRequired = v.IsRequired,
                ValidationPattern = v.ValidationPattern,
                ValidationMessage = v.ValidationMessage
            });

            var template = await _templateService.UpdateTemplateAsync(
                templateId,
                request.Name,
                request.Description,
                request.Content,
                request.MimeType,
                request.FileExtension,
                variables,
                request.DefaultTitlePattern,
                request.DefaultDescriptionPattern,
                request.Metadata,
                request.Tags,
                request.IsActive,
                updatedBy,
                cancellationToken);

            return Ok(MapTemplateToResponse(template));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when updating template");
            return BadRequest(new { error = "InvalidOperation", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update template {TemplateId}", templateId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while updating the template." });
        }
    }

    /// <summary>
    /// Deletes a template (soft delete).
    /// </summary>
    /// <param name="templateId">The template identifier.</param>
    /// <param name="deletedReason">Optional reason for deletion.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    /// <response code="204">Template successfully deleted.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the template is not found.</response>
    [HttpDelete("{templateId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTemplate(
        int templateId,
        [FromQuery] string? deletedReason = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify the template exists and belongs to user's tenant
            var template = await _templateService.GetTemplateByIdAsync(templateId, cancellationToken);
            if (template == null)
            {
                return NotFound(new { error = "TemplateNotFound", message = $"Template with ID {templateId} not found." });
            }

            var userTenantId = User.GetTenantId();
            if (template.TenantId != userTenantId)
            {
                return NotFound(new { error = "TemplateNotFound", message = $"Template with ID {templateId} not found." });
            }

            var deletedBy = User.Identity?.Name ?? "system";

            await _templateService.DeleteTemplateAsync(templateId, deletedBy, deletedReason, cancellationToken);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when deleting template");
            return BadRequest(new { error = "InvalidOperation", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete template {TemplateId}", templateId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while deleting the template." });
        }
    }

    /// <summary>
    /// Restores a soft-deleted template.
    /// </summary>
    /// <param name="templateId">The template identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    /// <response code="204">Template successfully restored.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the template is not found.</response>
    [HttpPost("{templateId}/restore")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestoreTemplate(int templateId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Need to check with includeDeleted
            var userTenantId = User.GetTenantId() ?? 0;
            var templates = await _templateService.GetTemplatesByTenantAsync(userTenantId, true, cancellationToken);
            var template = templates.FirstOrDefault(t => t.Id == templateId);

            if (template == null)
            {
                return NotFound(new { error = "TemplateNotFound", message = $"Template with ID {templateId} not found." });
            }

            await _templateService.RestoreTemplateAsync(templateId, cancellationToken);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when restoring template");
            return BadRequest(new { error = "InvalidOperation", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore template {TemplateId}", templateId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while restoring the template." });
        }
    }

    /// <summary>
    /// Creates a document from a template with variable substitution.
    /// </summary>
    /// <param name="templateId">The template identifier.</param>
    /// <param name="request">The document generation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created document.</returns>
    /// <response code="201">Returns the newly created document.</response>
    /// <response code="400">If the request is invalid or variable validation fails.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the template is not found.</response>
    [HttpPost("{templateId}/generate")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateDocumentFromTemplate(
        int templateId,
        [FromBody] GenerateDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify the template exists and belongs to user's tenant
            var template = await _templateService.GetTemplateByIdAsync(templateId, cancellationToken);
            if (template == null)
            {
                return NotFound(new { error = "TemplateNotFound", message = $"Template with ID {templateId} not found." });
            }

            var userTenantId = User.GetTenantId();
            if (template.TenantId != userTenantId)
            {
                return NotFound(new { error = "TemplateNotFound", message = $"Template with ID {templateId} not found." });
            }

            var createdBy = User.Identity?.Name ?? "system";

            var variables = request.Variables ?? new Dictionary<string, string>();

            var document = await _templateService.CreateDocumentFromTemplateAsync(
                templateId,
                variables,
                request.Title,
                request.Description,
                request.Metadata,
                request.Tags,
                createdBy,
                cancellationToken);

            return CreatedAtAction(
                "GetDocumentById",
                "Documents",
                new { documentId = document.Id },
                new
                {
                    id = document.Id,
                    tenantId = document.TenantId,
                    documentTypeId = document.DocumentTypeId,
                    title = document.Title,
                    description = document.Description,
                    originalFileName = document.OriginalFileName,
                    mimeType = document.MimeType,
                    fileSizeBytes = document.FileSizeBytes,
                    contentHash = document.ContentHash,
                    metadata = document.Metadata,
                    tags = document.Tags,
                    version = document.Version,
                    isCurrentVersion = document.IsCurrentVersion,
                    createdAt = document.CreatedAt,
                    createdBy = document.CreatedBy
                });
        }
        catch (ArgumentException ex) when (ex.Message.Contains("Variable validation failed"))
        {
            _logger.LogWarning(ex, "Variable validation failed when generating document from template");
            return BadRequest(new { error = "ValidationError", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when generating document from template");
            return BadRequest(new { error = "InvalidOperation", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate document from template {TemplateId}", templateId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while generating the document." });
        }
    }

    /// <summary>
    /// Validates variable values against a template without creating a document.
    /// </summary>
    /// <param name="templateId">The template identifier.</param>
    /// <param name="request">The validation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result.</returns>
    /// <response code="200">Returns the validation result.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the template is not found.</response>
    [HttpPost("{templateId}/validate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ValidateVariables(
        int templateId,
        [FromBody] ValidateVariablesRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var template = await _templateService.GetTemplateByIdAsync(templateId, cancellationToken);
            if (template == null)
            {
                return NotFound(new { error = "TemplateNotFound", message = $"Template with ID {templateId} not found." });
            }

            var userTenantId = User.GetTenantId();
            if (template.TenantId != userTenantId)
            {
                return NotFound(new { error = "TemplateNotFound", message = $"Template with ID {templateId} not found." });
            }

            var variables = request.Variables ?? new Dictionary<string, string>();
            var validationErrors = _templateService.ValidateVariables(template, variables);

            return Ok(new
            {
                isValid = !validationErrors.Any(),
                errors = validationErrors
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate variables for template {TemplateId}", templateId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while validating variables." });
        }
    }

    private static object MapTemplateToResponse(DocumentTemplate template)
    {
        return new
        {
            id = template.Id,
            tenantId = template.TenantId,
            documentTypeId = template.DocumentTypeId,
            name = template.Name,
            description = template.Description,
            content = template.Content,
            mimeType = template.MimeType,
            fileExtension = template.FileExtension,
            isActive = template.IsActive,
            defaultTitlePattern = template.DefaultTitlePattern,
            defaultDescriptionPattern = template.DefaultDescriptionPattern,
            metadata = template.Metadata,
            tags = template.Tags,
            variables = template.Variables?.OrderBy(v => v.SortOrder).Select(v => new
            {
                id = v.Id,
                name = v.Name,
                displayName = v.DisplayName,
                description = v.Description,
                dataType = v.DataType,
                defaultValue = v.DefaultValue,
                isRequired = v.IsRequired,
                validationPattern = v.ValidationPattern,
                validationMessage = v.ValidationMessage,
                sortOrder = v.SortOrder
            }),
            createdAt = template.CreatedAt,
            createdBy = template.CreatedBy,
            updatedAt = template.UpdatedAt,
            updatedBy = template.UpdatedBy,
            isDeleted = template.IsDeleted
        };
    }

    private static object MapTemplateToListResponse(DocumentTemplate template)
    {
        return new
        {
            id = template.Id,
            tenantId = template.TenantId,
            documentTypeId = template.DocumentTypeId,
            name = template.Name,
            description = template.Description,
            mimeType = template.MimeType,
            fileExtension = template.FileExtension,
            isActive = template.IsActive,
            variableCount = template.Variables?.Count ?? 0,
            createdAt = template.CreatedAt,
            createdBy = template.CreatedBy,
            updatedAt = template.UpdatedAt,
            isDeleted = template.IsDeleted
        };
    }
}

/// <summary>
/// Request model for creating a template.
/// </summary>
public class CreateTemplateRequest
{
    public int TenantId { get; set; }
    public int DocumentTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? MimeType { get; set; }
    public string? FileExtension { get; set; }
    public string? DefaultTitlePattern { get; set; }
    public string? DefaultDescriptionPattern { get; set; }
    public string? Metadata { get; set; }
    public string? Tags { get; set; }
    public List<TemplateVariableRequest>? Variables { get; set; }
}

/// <summary>
/// Request model for updating a template.
/// </summary>
public class UpdateTemplateRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Content { get; set; }
    public string? MimeType { get; set; }
    public string? FileExtension { get; set; }
    public string? DefaultTitlePattern { get; set; }
    public string? DefaultDescriptionPattern { get; set; }
    public string? Metadata { get; set; }
    public string? Tags { get; set; }
    public bool? IsActive { get; set; }
    public List<TemplateVariableRequest>? Variables { get; set; }
}

/// <summary>
/// Request model for a template variable.
/// </summary>
public class TemplateVariableRequest
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DataType { get; set; }
    public string? DefaultValue { get; set; }
    public bool IsRequired { get; set; }
    public string? ValidationPattern { get; set; }
    public string? ValidationMessage { get; set; }
}

/// <summary>
/// Request model for generating a document from a template.
/// </summary>
public class GenerateDocumentRequest
{
    public Dictionary<string, string>? Variables { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Metadata { get; set; }
    public string? Tags { get; set; }
}

/// <summary>
/// Request model for validating variables.
/// </summary>
public class ValidateVariablesRequest
{
    public Dictionary<string, string>? Variables { get; set; }
}
