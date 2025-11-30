using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskMaster.DocumentService.Api.Authorization;
using TaskMaster.DocumentService.Api.Extensions;
using TaskMaster.DocumentService.Core.Interfaces;
using TaskMaster.DocumentService.SDK.DTOs;

namespace TaskMaster.DocumentService.Api.Controllers;

/// <summary>
/// Controller for document template management operations.
/// Provides endpoints for creating, managing, and generating documents from tenant-specific templates.
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
    /// <param name="request">The template creation request.</param>
    /// <param name="file">The template file to upload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created template information.</returns>
    /// <response code="201">Returns the created template.</response>
    /// <response code="400">If the request is invalid.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user does not have access to the specified tenant.</response>
    [HttpPost]
    [TenantAuthorization("request.TenantId")]
    [ProducesResponseType(typeof(DocumentTemplateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateTemplate(
        [FromForm] CreateTemplateRequest request,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "InvalidFile", message = "Template file is required." });
        }

        try
        {
            var userId = User.Identity?.Name ?? "anonymous";

            _logger.LogInformation(
                "User {UserId} creating template '{TemplateName}' for tenant {TenantId}",
                userId, request.Name, request.TenantId);

            await using var stream = file.OpenReadStream();
            var template = await _templateService.CreateTemplateAsync(
                request.TenantId,
                request.DocumentTypeId,
                request.Name,
                request.Description,
                stream,
                file.FileName,
                file.ContentType,
                request.AvailableVariables,
                request.Metadata,
                request.Category,
                userId,
                cancellationToken);

            var dto = MapToDto(template);

            return CreatedAtAction(
                nameof(GetTemplate),
                new { templateId = template.Id },
                dto);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while creating template");
            return BadRequest(new { error = "InvalidOperation", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating template");
            return StatusCode(500, new { error = "InternalError", message = "An error occurred while creating the template." });
        }
    }

    /// <summary>
    /// Gets a template by its identifier.
    /// </summary>
    /// <param name="templateId">The template identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The template information.</returns>
    /// <response code="200">Returns the template.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the template is not found.</response>
    [HttpGet("{templateId}")]
    [ProducesResponseType(typeof(DocumentTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTemplate(long templateId, CancellationToken cancellationToken)
    {
        var template = await _templateService.GetTemplateByIdAsync(templateId, cancellationToken);

        if (template == null)
        {
            return NotFound(new { error = "TemplateNotFound", message = $"Template with ID {templateId} not found." });
        }

        // Verify tenant authorization
        var userTenantId = User.GetTenantId();
        if (template.TenantId != userTenantId)
        {
            _logger.LogWarning(
                "User from tenant {UserTenantId} attempted to access template {TemplateId} from tenant {TemplateTenantId}",
                userTenantId, templateId, template.TenantId);
            return Forbid();
        }

        return Ok(MapToDto(template));
    }

    /// <summary>
    /// Gets all templates for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="includeInactive">Whether to include inactive templates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of templates.</returns>
    /// <response code="200">Returns the list of templates.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user does not have access to the specified tenant.</response>
    [HttpGet("tenant/{tenantId}")]
    [TenantAuthorization("tenantId")]
    [ProducesResponseType(typeof(IEnumerable<DocumentTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTemplatesByTenant(
        int tenantId,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var templates = await _templateService.GetTemplatesByTenantAsync(
            tenantId,
            includeDeleted: false,
            includeInactive,
            cancellationToken);

        var dtos = templates.Select(MapToDto);
        return Ok(dtos);
    }

    /// <summary>
    /// Gets templates by category.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="category">The category name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of templates in the specified category.</returns>
    /// <response code="200">Returns the list of templates.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user does not have access to the specified tenant.</response>
    [HttpGet("tenant/{tenantId}/category/{category}")]
    [TenantAuthorization("tenantId")]
    [ProducesResponseType(typeof(IEnumerable<DocumentTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTemplatesByCategory(
        int tenantId,
        string category,
        CancellationToken cancellationToken)
    {
        var templates = await _templateService.GetTemplatesByCategoryAsync(tenantId, category, cancellationToken);
        var dtos = templates.Select(MapToDto);
        return Ok(dtos);
    }

    /// <summary>
    /// Searches templates by name.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="searchTerm">The search term.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of matching templates.</returns>
    /// <response code="200">Returns the list of matching templates.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user does not have access to the specified tenant.</response>
    [HttpGet("tenant/{tenantId}/search")]
    [TenantAuthorization("tenantId")]
    [ProducesResponseType(typeof(IEnumerable<DocumentTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SearchTemplates(
        int tenantId,
        [FromQuery] string searchTerm,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return BadRequest(new { error = "InvalidSearchTerm", message = "Search term cannot be empty." });
        }

        var templates = await _templateService.SearchTemplatesAsync(tenantId, searchTerm, cancellationToken);
        var dtos = templates.Select(MapToDto);
        return Ok(dtos);
    }

    /// <summary>
    /// Updates template metadata.
    /// </summary>
    /// <param name="templateId">The template identifier.</param>
    /// <param name="request">The update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated template information.</returns>
    /// <response code="200">Returns the updated template.</response>
    /// <response code="400">If the request is invalid.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the template is not found.</response>
    [HttpPut("{templateId}")]
    [ProducesResponseType(typeof(DocumentTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTemplate(
        long templateId,
        [FromBody] UpdateTemplateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // First get the template to verify tenant authorization
            var existingTemplate = await _templateService.GetTemplateByIdAsync(templateId, cancellationToken);
            if (existingTemplate == null)
            {
                return NotFound(new { error = "TemplateNotFound", message = $"Template with ID {templateId} not found." });
            }

            var userTenantId = User.GetTenantId();
            if (existingTemplate.TenantId != userTenantId)
            {
                _logger.LogWarning(
                    "User from tenant {UserTenantId} attempted to update template {TemplateId} from tenant {TemplateTenantId}",
                    userTenantId, templateId, existingTemplate.TenantId);
                return Forbid();
            }

            var userId = User.Identity?.Name ?? "anonymous";

            var template = await _templateService.UpdateTemplateMetadataAsync(
                templateId,
                request.Name,
                request.Description,
                request.AvailableVariables,
                request.Metadata,
                request.Category,
                request.IsActive,
                userId,
                cancellationToken);

            return Ok(MapToDto(template));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while updating template {TemplateId}", templateId);
            return BadRequest(new { error = "InvalidOperation", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating template {TemplateId}", templateId);
            return StatusCode(500, new { error = "InternalError", message = "An error occurred while updating the template." });
        }
    }

    /// <summary>
    /// Downloads a template file.
    /// </summary>
    /// <param name="templateId">The template identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The template file.</returns>
    /// <response code="200">Returns the template file.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the template is not found.</response>
    [HttpGet("{templateId}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadTemplate(long templateId, CancellationToken cancellationToken)
    {
        try
        {
            var template = await _templateService.GetTemplateByIdAsync(templateId, cancellationToken);
            if (template == null)
            {
                return NotFound(new { error = "TemplateNotFound", message = $"Template with ID {templateId} not found." });
            }

            // Verify tenant authorization
            var userTenantId = User.GetTenantId();
            if (template.TenantId != userTenantId)
            {
                return Forbid();
            }

            var stream = await _templateService.DownloadTemplateAsync(templateId, cancellationToken);
            return File(stream, template.MimeType, template.OriginalFileName ?? "template");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading template {TemplateId}", templateId);
            return StatusCode(500, new { error = "InternalError", message = "An error occurred while downloading the template." });
        }
    }

    /// <summary>
    /// Generates a document from a template with variable substitution.
    /// Variables in the template should use the format {{variableName}}.
    /// </summary>
    /// <param name="request">The document generation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated document information.</returns>
    /// <response code="201">Returns the generated document.</response>
    /// <response code="400">If the request is invalid.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the template is not found.</response>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateDocumentFromTemplate(
        [FromBody] GenerateDocumentFromTemplateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // First get the template to verify tenant authorization
            var template = await _templateService.GetTemplateByIdAsync(request.TemplateId, cancellationToken);
            if (template == null)
            {
                return NotFound(new { error = "TemplateNotFound", message = $"Template with ID {request.TemplateId} not found." });
            }

            var userTenantId = User.GetTenantId();
            if (template.TenantId != userTenantId)
            {
                _logger.LogWarning(
                    "User from tenant {UserTenantId} attempted to generate document from template {TemplateId} from tenant {TemplateTenantId}",
                    userTenantId, request.TemplateId, template.TenantId);
                return Forbid();
            }

            var userId = User.Identity?.Name ?? "anonymous";

            _logger.LogInformation(
                "User {UserId} generating document '{Title}' from template {TemplateId}",
                userId, request.Title, request.TemplateId);

            var document = await _templateService.GenerateDocumentFromTemplateAsync(
                request.TemplateId,
                request.Title,
                request.Description,
                request.Variables,
                request.Metadata,
                request.Tags,
                userId,
                cancellationToken);

            var dto = MapDocumentToDto(document);

            return CreatedAtAction(
                "GetDocument",
                "Documents",
                new { documentId = document.Id },
                dto);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while generating document from template");
            return BadRequest(new { error = "InvalidOperation", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating document from template {TemplateId}", request.TemplateId);
            return StatusCode(500, new { error = "InternalError", message = "An error occurred while generating the document." });
        }
    }

    /// <summary>
    /// Soft-deletes a template.
    /// </summary>
    /// <param name="templateId">The template identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    /// <response code="204">Template successfully deleted.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the template is not found.</response>
    [HttpDelete("{templateId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTemplate(long templateId, CancellationToken cancellationToken)
    {
        try
        {
            var template = await _templateService.GetTemplateByIdAsync(templateId, cancellationToken);
            if (template == null)
            {
                return NotFound(new { error = "TemplateNotFound", message = $"Template with ID {templateId} not found." });
            }

            // Verify tenant authorization
            var userTenantId = User.GetTenantId();
            if (template.TenantId != userTenantId)
            {
                return Forbid();
            }

            var userId = User.Identity?.Name ?? "anonymous";
            await _templateService.DeleteTemplateAsync(templateId, userId, cancellationToken);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting template {TemplateId}", templateId);
            return StatusCode(500, new { error = "InternalError", message = "An error occurred while deleting the template." });
        }
    }

    private static DocumentTemplateDto MapToDto(Core.Entities.DocumentTemplate template)
    {
        return new DocumentTemplateDto
        {
            Id = template.Id,
            TenantId = template.TenantId,
            DocumentTypeId = template.DocumentTypeId,
            Name = template.Name,
            Description = template.Description,
            BlobPath = template.BlobPath,
            FileSizeBytes = template.FileSizeBytes,
            MimeType = template.MimeType,
            OriginalFileName = template.OriginalFileName,
            AvailableVariables = template.AvailableVariables,
            Metadata = template.Metadata,
            Category = template.Category,
            Version = template.Version,
            ParentTemplateId = template.ParentTemplateId,
            IsCurrentVersion = template.IsCurrentVersion,
            IsActive = template.IsActive,
            CreatedAt = template.CreatedAt,
            CreatedBy = template.CreatedBy,
            UpdatedAt = template.UpdatedAt,
            UpdatedBy = template.UpdatedBy,
            IsDeleted = template.IsDeleted
        };
    }

    private static DocumentDto MapDocumentToDto(Core.Entities.Document document)
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
            CreatedAt = document.CreatedAt,
            CreatedBy = document.CreatedBy,
            UpdatedAt = document.UpdatedAt,
            UpdatedBy = document.UpdatedBy,
            IsDeleted = document.IsDeleted,
            IsArchived = document.IsArchived
        };
    }
}
