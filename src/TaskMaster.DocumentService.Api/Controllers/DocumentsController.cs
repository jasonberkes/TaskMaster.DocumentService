using Microsoft.AspNetCore.Mvc;
using TaskMaster.DocumentService.Core.DTOs;
using TaskMaster.DocumentService.Core.Services;

namespace TaskMaster.DocumentService.Api.Controllers;

/// <summary>
/// Controller for document operations including search.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentSearchService _searchService;
    private readonly ILogger<DocumentsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentsController"/> class.
    /// </summary>
    /// <param name="searchService">Document search service.</param>
    /// <param name="logger">Logger instance.</param>
    public DocumentsController(
        IDocumentSearchService searchService,
        ILogger<DocumentsController> logger)
    {
        _searchService = searchService;
        _logger = logger;
    }

    /// <summary>
    /// Searches for documents using full-text search.
    /// </summary>
    /// <param name="request">The search request parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Search results containing matching documents.</returns>
    /// <response code="200">Returns the search results.</response>
    /// <response code="400">If the request is invalid.</response>
    /// <response code="500">If an error occurs during search.</response>
    [HttpPost("search")]
    [ProducesResponseType(typeof(DocumentSearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DocumentSearchResponse>> Search(
        [FromBody] DocumentSearchRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Invalid search request",
                    Detail = "Search query cannot be empty."
                });
            }

            if (request.Page < 1)
            {
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Invalid search request",
                    Detail = "Page number must be greater than 0."
                });
            }

            if (request.PageSize < 1 || request.PageSize > 100)
            {
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Invalid search request",
                    Detail = "Page size must be between 1 and 100."
                });
            }

            var results = await _searchService.SearchAsync(request, cancellationToken);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching documents");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Search error",
                Detail = "An error occurred while searching documents."
            });
        }
    }

    /// <summary>
    /// Indexes a specific document for search.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Document indexed successfully.</response>
    /// <response code="404">If the document is not found.</response>
    /// <response code="500">If an error occurs during indexing.</response>
    [HttpPost("{documentId}/index")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> IndexDocument(
        [FromRoute] long documentId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _searchService.IndexDocumentAsync(documentId, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Document {DocumentId} not found for indexing", documentId);
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Document not found",
                Detail = $"Document {documentId} not found."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing document {DocumentId}", documentId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Indexing error",
                Detail = "An error occurred while indexing the document."
            });
        }
    }

    /// <summary>
    /// Reindexes all documents for a specific tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of documents reindexed.</returns>
    /// <response code="200">Returns the number of documents reindexed.</response>
    /// <response code="500">If an error occurs during reindexing.</response>
    [HttpPost("reindex/tenant/{tenantId}")]
    [ProducesResponseType(typeof(ReindexResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ReindexResponse>> ReindexTenant(
        [FromRoute] int tenantId,
        CancellationToken cancellationToken)
    {
        try
        {
            var count = await _searchService.ReindexTenantDocumentsAsync(tenantId, cancellationToken);
            return Ok(new ReindexResponse
            {
                TenantId = tenantId,
                DocumentsReindexed = count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reindexing documents for tenant {TenantId}", tenantId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Reindexing error",
                Detail = "An error occurred while reindexing documents."
            });
        }
    }

    /// <summary>
    /// Removes a document from the search index.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Document removed from index successfully.</response>
    /// <response code="500">If an error occurs during removal.</response>
    [HttpDelete("{documentId}/index")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RemoveFromIndex(
        [FromRoute] long documentId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _searchService.RemoveFromIndexAsync(documentId, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing document {DocumentId} from index", documentId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Index removal error",
                Detail = "An error occurred while removing the document from the index."
            });
        }
    }
}

/// <summary>
/// Response for reindex operations.
/// </summary>
public class ReindexResponse
{
    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    public int TenantId { get; set; }

    /// <summary>
    /// Gets or sets the number of documents that were reindexed.
    /// </summary>
    public int DocumentsReindexed { get; set; }
}
