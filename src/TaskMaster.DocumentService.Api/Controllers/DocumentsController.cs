using Microsoft.AspNetCore.Mvc;
using TaskMaster.DocumentService.Core.DTOs;
using TaskMaster.DocumentService.Core.Interfaces;

namespace TaskMaster.DocumentService.Api.Controllers;

/// <summary>
/// API controller for document management operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly ILogger<DocumentsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentsController"/> class
    /// </summary>
    /// <param name="documentService">The document service</param>
    /// <param name="logger">The logger instance</param>
    public DocumentsController(IDocumentService documentService, ILogger<DocumentsController> logger)
    {
        _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets a document by its identifier
    /// </summary>
    /// <param name="id">The document identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The document if found</returns>
    /// <response code="200">Returns the document</response>
    /// <response code="404">Document not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentDto>> GetById(long id, CancellationToken cancellationToken)
    {
        try
        {
            var document = await _documentService.GetByIdAsync(id, cancellationToken);
            if (document == null)
            {
                return NotFound(new { message = $"Document with ID {id} not found" });
            }

            return Ok(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document {DocumentId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving the document" });
        }
    }

    /// <summary>
    /// Gets documents by tenant identifier with pagination
    /// </summary>
    /// <param name="tenantId">The tenant identifier</param>
    /// <param name="skip">Number of records to skip (default: 0)</param>
    /// <param name="take">Number of records to take (default: 50, max: 100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of documents</returns>
    /// <response code="200">Returns the list of documents</response>
    [HttpGet("tenant/{tenantId}")]
    [ProducesResponseType(typeof(IEnumerable<DocumentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<DocumentDto>>> GetByTenantId(
        int tenantId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate pagination parameters
            if (skip < 0) skip = 0;
            if (take < 1 || take > 100) take = 50;

            var documents = await _documentService.GetByTenantIdAsync(tenantId, skip, take, cancellationToken);
            return Ok(documents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving documents for tenant {TenantId}", tenantId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving documents" });
        }
    }

    /// <summary>
    /// Gets all versions of a document
    /// </summary>
    /// <param name="id">The document identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of document versions</returns>
    /// <response code="200">Returns the list of versions</response>
    [HttpGet("{id}/versions")]
    [ProducesResponseType(typeof(IEnumerable<DocumentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<DocumentDto>>> GetVersions(long id, CancellationToken cancellationToken)
    {
        try
        {
            var versions = await _documentService.GetVersionsAsync(id, cancellationToken);
            return Ok(versions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving versions for document {DocumentId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving document versions" });
        }
    }

    /// <summary>
    /// Gets the count of documents for a tenant
    /// </summary>
    /// <param name="tenantId">The tenant identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The count of documents</returns>
    /// <response code="200">Returns the count</response>
    [HttpGet("tenant/{tenantId}/count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<ActionResult<int>> GetCountByTenantId(int tenantId, CancellationToken cancellationToken)
    {
        try
        {
            var count = await _documentService.GetCountByTenantIdAsync(tenantId, cancellationToken);
            return Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document count for tenant {TenantId}", tenantId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving the document count" });
        }
    }

    /// <summary>
    /// Creates a new document
    /// </summary>
    /// <param name="createDto">The document creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created document</returns>
    /// <response code="201">Document created successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="409">Duplicate document detected</response>
    [HttpPost]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DocumentDto>> Create([FromBody] CreateDocumentDto createDto, CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var document = await _documentService.CreateAsync(createDto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = document.Id }, document);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Duplicate document detected");
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating document");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while creating the document" });
        }
    }

    /// <summary>
    /// Creates a new version of an existing document
    /// </summary>
    /// <param name="id">The original document identifier</param>
    /// <param name="versionDto">The version creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created version</returns>
    /// <response code="201">Version created successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="404">Original document not found</response>
    [HttpPost("{id}/versions")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentDto>> CreateVersion(
        long id,
        [FromBody] CreateDocumentVersionDto versionDto,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var version = await _documentService.CreateVersionAsync(id, versionDto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = version.Id }, version);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error creating version for document {DocumentId}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating version for document {DocumentId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while creating the document version" });
        }
    }

    /// <summary>
    /// Updates document metadata
    /// </summary>
    /// <param name="id">The document identifier</param>
    /// <param name="updateDto">The update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated document</returns>
    /// <response code="200">Document updated successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="404">Document not found</response>
    [HttpPatch("{id}")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentDto>> Update(
        long id,
        [FromBody] UpdateDocumentDto updateDto,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var document = await _documentService.UpdateAsync(id, updateDto, cancellationToken);
            if (document == null)
            {
                return NotFound(new { message = $"Document with ID {id} not found" });
            }

            return Ok(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document {DocumentId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while updating the document" });
        }
    }

    /// <summary>
    /// Soft deletes a document
    /// </summary>
    /// <param name="id">The document identifier</param>
    /// <param name="deletedBy">The user performing the deletion</param>
    /// <param name="deletedReason">The reason for deletion</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Document deleted successfully</response>
    /// <response code="404">Document not found</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        long id,
        [FromQuery] string? deletedBy = null,
        [FromQuery] string? deletedReason = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _documentService.DeleteAsync(id, deletedBy, deletedReason, cancellationToken);
            if (!result)
            {
                return NotFound(new { message = $"Document with ID {id} not found" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {DocumentId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while deleting the document" });
        }
    }

    /// <summary>
    /// Archives a document
    /// </summary>
    /// <param name="id">The document identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Document archived successfully</response>
    /// <response code="404">Document not found</response>
    [HttpPost("{id}/archive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Archive(long id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _documentService.ArchiveAsync(id, cancellationToken);
            if (!result)
            {
                return NotFound(new { message = $"Document with ID {id} not found" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving document {DocumentId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while archiving the document" });
        }
    }

    /// <summary>
    /// Unarchives a document
    /// </summary>
    /// <param name="id">The document identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Document unarchived successfully</response>
    /// <response code="404">Document not found</response>
    [HttpPost("{id}/unarchive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unarchive(long id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _documentService.UnarchiveAsync(id, cancellationToken);
            if (!result)
            {
                return NotFound(new { message = $"Document with ID {id} not found" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unarchiving document {DocumentId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while unarchiving the document" });
        }
    }
}
