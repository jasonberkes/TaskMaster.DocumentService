using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TaskMaster.DocumentService.Core.DTOs;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Interfaces;

namespace TaskMaster.DocumentService.Api.Controllers;

/// <summary>
/// API controller for managing document templates and variable substitution.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TemplatesController : ControllerBase
{
    private readonly ITemplateService _templateService;
    private readonly ILogger<TemplatesController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TemplatesController"/> class.
    /// </summary>
    /// <param name="templateService">The template service.</param>
    /// <param name="logger">The logger.</param>
    public TemplatesController(ITemplateService templateService, ILogger<TemplatesController> logger)
    {
        _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets a template by identifier.
    /// </summary>
    /// <param name="id">The template identifier.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The template if found.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TemplateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTemplate(int id, [FromQuery] int tenantId, CancellationToken cancellationToken)
    {
        try
        {
            var template = await _templateService.GetTemplateAsync(id, tenantId, cancellationToken);

            if (template == null)
            {
                return NotFound(new { message = "Template not found or access denied" });
            }

            var response = MapToResponse(template);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving template {TemplateId} for tenant {TenantId}", id, tenantId);
            return StatusCode(500, new { message = "An error occurred while retrieving the template" });
        }
    }

    /// <summary>
    /// Gets all templates for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="includePublic">Whether to include public templates.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of templates.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<TemplateResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTemplates([FromQuery] int tenantId, [FromQuery] bool includePublic = true, CancellationToken cancellationToken = default)
    {
        try
        {
            var templates = await _templateService.GetTemplatesAsync(tenantId, includePublic, cancellationToken);
            var responses = templates.Select(MapToResponse).ToList();
            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving templates for tenant {TenantId}", tenantId);
            return StatusCode(500, new { message = "An error occurred while retrieving templates" });
        }
    }

    /// <summary>
    /// Gets templates by type.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="type">The template type.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of templates.</returns>
    [HttpGet("by-type/{type}")]
    [ProducesResponseType(typeof(List<TemplateResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTemplatesByType([FromQuery] int tenantId, string type, CancellationToken cancellationToken)
    {
        try
        {
            var templates = await _templateService.GetTemplatesByTypeAsync(tenantId, type, cancellationToken);
            var responses = templates.Select(MapToResponse).ToList();
            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving templates by type {Type} for tenant {TenantId}", type, tenantId);
            return StatusCode(500, new { message = "An error occurred while retrieving templates" });
        }
    }

    /// <summary>
    /// Searches templates by name.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="searchTerm">The search term.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of matching templates.</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(List<TemplateResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SearchTemplates([FromQuery] int tenantId, [FromQuery] string searchTerm, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return BadRequest(new { message = "Search term is required" });
        }

        try
        {
            var templates = await _templateService.SearchTemplatesAsync(tenantId, searchTerm, cancellationToken);
            var responses = templates.Select(MapToResponse).ToList();
            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching templates for tenant {TenantId} with term {SearchTerm}", tenantId, searchTerm);
            return StatusCode(500, new { message = "An error occurred while searching templates" });
        }
    }

    /// <summary>
    /// Creates a new template.
    /// </summary>
    /// <param name="request">The create template request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created template.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(TemplateResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateTemplateRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // Validate template
            var validationErrors = _templateService.ValidateTemplate(request.Content, request.Variables);
            if (validationErrors.Count > 0)
            {
                return BadRequest(new { message = "Template validation failed", errors = validationErrors });
            }

            var template = new DocumentTemplate
            {
                TenantId = request.TenantId,
                Name = request.Name,
                Description = request.Description,
                Content = request.Content,
                TemplateType = request.TemplateType,
                Category = request.Category,
                Variables = JsonSerializer.Serialize(request.Variables),
                IsPublic = request.IsPublic
            };

            var createdBy = User?.Identity?.Name ?? "system";
            var created = await _templateService.CreateTemplateAsync(template, createdBy, cancellationToken);

            var response = MapToResponse(created);
            return CreatedAtAction(nameof(GetTemplate), new { id = created.Id, tenantId = created.TenantId }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating template for tenant {TenantId}", request.TenantId);
            return StatusCode(500, new { message = "An error occurred while creating the template" });
        }
    }

    /// <summary>
    /// Updates an existing template.
    /// </summary>
    /// <param name="id">The template identifier.</param>
    /// <param name="request">The update template request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated template.</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(TemplateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateTemplate(int id, [FromBody] UpdateTemplateRequest request, CancellationToken cancellationToken)
    {
        if (id != request.Id)
        {
            return BadRequest(new { message = "Template ID mismatch" });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // Validate template
            var validationErrors = _templateService.ValidateTemplate(request.Content, request.Variables);
            if (validationErrors.Count > 0)
            {
                return BadRequest(new { message = "Template validation failed", errors = validationErrors });
            }

            // Get existing template (for tenantId and authorization)
            var existing = await _templateService.GetTemplateAsync(id, 0, cancellationToken);
            if (existing == null)
            {
                return NotFound(new { message = "Template not found" });
            }

            existing.Name = request.Name;
            existing.Description = request.Description;
            existing.Content = request.Content;
            existing.TemplateType = request.TemplateType;
            existing.Category = request.Category;
            existing.Variables = JsonSerializer.Serialize(request.Variables);
            existing.IsPublic = request.IsPublic;
            existing.IsActive = request.IsActive;

            var updatedBy = User?.Identity?.Name ?? "system";
            var updated = await _templateService.UpdateTemplateAsync(existing, updatedBy, cancellationToken);

            var response = MapToResponse(updated);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating template {TemplateId}", id);
            return StatusCode(500, new { message = "An error occurred while updating the template" });
        }
    }

    /// <summary>
    /// Deletes a template.
    /// </summary>
    /// <param name="id">The template identifier.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteTemplate(int id, [FromQuery] int tenantId, CancellationToken cancellationToken)
    {
        try
        {
            var deletedBy = User?.Identity?.Name ?? "system";
            var deleted = await _templateService.DeleteTemplateAsync(id, tenantId, deletedBy, cancellationToken);

            if (!deleted)
            {
                return NotFound(new { message = "Template not found or access denied" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting template {TemplateId} for tenant {TenantId}", id, tenantId);
            return StatusCode(500, new { message = "An error occurred while deleting the template" });
        }
    }

    /// <summary>
    /// Renders a template with variable substitution.
    /// </summary>
    /// <param name="request">The render template request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The rendered template content.</returns>
    [HttpPost("render")]
    [ProducesResponseType(typeof(RenderTemplateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RenderTemplate([FromBody] RenderTemplateRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var usedBy = User?.Identity?.Name ?? "system";
            var result = await _templateService.RenderTemplateAsync(
                request.TemplateId,
                request.TenantId,
                request.Variables,
                usedBy,
                request.DocumentId,
                cancellationToken);

            var response = new RenderTemplateResponse
            {
                RenderedContent = result.RenderedContent,
                IsSuccess = result.IsSuccess,
                Warnings = result.Warnings,
                Errors = result.Errors,
                SubstitutedVariables = result.SubstitutedVariables,
                MissingVariables = result.MissingVariables
            };

            if (result.Errors.Count > 0 && result.Errors.Contains("Template not found or access denied"))
            {
                return NotFound(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering template {TemplateId} for tenant {TenantId}", request.TemplateId, request.TenantId);
            return StatusCode(500, new { message = "An error occurred while rendering the template" });
        }
    }

    private TemplateResponse MapToResponse(DocumentTemplate template)
    {
        var variables = _templateService.ParseTemplateVariables(template.Variables);

        return new TemplateResponse
        {
            Id = template.Id,
            TenantId = template.TenantId,
            Name = template.Name,
            Description = template.Description,
            Content = template.Content,
            TemplateType = template.TemplateType,
            Category = template.Category,
            Variables = variables,
            IsPublic = template.IsPublic,
            Version = template.Version,
            IsCurrentVersion = template.IsCurrentVersion,
            CreatedAt = template.CreatedAt,
            CreatedBy = template.CreatedBy,
            UpdatedAt = template.UpdatedAt,
            UpdatedBy = template.UpdatedBy,
            IsActive = template.IsActive
        };
    }
}
