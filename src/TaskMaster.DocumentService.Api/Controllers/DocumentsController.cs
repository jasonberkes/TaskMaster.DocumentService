using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskMaster.DocumentService.Core.Authorization;
using TaskMaster.DocumentService.Core.DTOs;
using TaskMaster.DocumentService.Core.Interfaces;

namespace TaskMaster.DocumentService.Api.Controllers;

/// <summary>
/// Controller for managing documents with tenant-scoped authorization.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.TenantScoped)]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly ILogger<DocumentsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentsController"/> class.
    /// </summary>
    public DocumentsController(IDocumentService documentService, ILogger<DocumentsController> logger)
    {
        _documentService = documentService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all documents for the authenticated tenant.
    /// </summary>
    /// <returns>A list of documents.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<DocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<DocumentDto>>> GetDocuments()
    {
        var tenantId = User.GetTenantId();

        if (tenantId == null)
        {
            _logger.LogWarning("Tenant ID not found in claims");
            return Forbid();
        }

        try
        {
            var documents = await _documentService.GetTenantDocumentsAsync(tenantId.Value);
            return Ok(documents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving documents for tenant {TenantId}", tenantId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving documents");
        }
    }

    /// <summary>
    /// Gets a specific document by ID.
    /// </summary>
    /// <param name="id">The document ID.</param>
    /// <returns>The document details.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentDto>> GetDocument(Guid id)
    {
        var tenantId = User.GetTenantId();

        if (tenantId == null)
        {
            _logger.LogWarning("Tenant ID not found in claims");
            return Forbid();
        }

        try
        {
            var document = await _documentService.GetDocumentAsync(id, tenantId.Value);

            if (document == null)
            {
                return NotFound();
            }

            return Ok(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document {DocumentId} for tenant {TenantId}", id, tenantId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the document");
        }
    }

    /// <summary>
    /// Creates a new document.
    /// </summary>
    /// <param name="createDto">The document creation data.</param>
    /// <returns>The created document.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentDto>> CreateDocument([FromBody] CreateDocumentDto createDto)
    {
        var tenantId = User.GetTenantId();

        if (tenantId == null)
        {
            _logger.LogWarning("Tenant ID not found in claims");
            return Forbid();
        }

        var userId = User.GetUserId();

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("User ID not found in claims");
            return Forbid();
        }

        try
        {
            var document = await _documentService.CreateDocumentAsync(createDto, tenantId.Value, userId);
            return CreatedAtAction(nameof(GetDocument), new { id = document.Id }, document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating document for tenant {TenantId}", tenantId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the document");
        }
    }

    /// <summary>
    /// Updates an existing document.
    /// </summary>
    /// <param name="id">The document ID.</param>
    /// <param name="updateDto">The document update data.</param>
    /// <returns>The updated document.</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentDto>> UpdateDocument(Guid id, [FromBody] UpdateDocumentDto updateDto)
    {
        var tenantId = User.GetTenantId();

        if (tenantId == null)
        {
            _logger.LogWarning("Tenant ID not found in claims");
            return Forbid();
        }

        try
        {
            var document = await _documentService.UpdateDocumentAsync(id, updateDto, tenantId.Value);

            if (document == null)
            {
                return NotFound();
            }

            return Ok(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document {DocumentId} for tenant {TenantId}", id, tenantId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the document");
        }
    }

    /// <summary>
    /// Deletes a document (soft delete).
    /// </summary>
    /// <param name="id">The document ID.</param>
    /// <returns>No content if successful.</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDocument(Guid id)
    {
        var tenantId = User.GetTenantId();

        if (tenantId == null)
        {
            _logger.LogWarning("Tenant ID not found in claims");
            return Forbid();
        }

        try
        {
            var result = await _documentService.DeleteDocumentAsync(id, tenantId.Value);

            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {DocumentId} for tenant {TenantId}", id, tenantId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the document");
        }
    }
}
