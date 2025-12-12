using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskMaster.DocumentService.Api.Authorization;
using TaskMaster.DocumentService.Api.Extensions;
using TaskMaster.DocumentService.Search.Interfaces;
using TaskMaster.DocumentService.Search.Models;

namespace TaskMaster.DocumentService.Api.Controllers;

/// <summary>
/// Controller for document search operations using Meilisearch.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;
    private readonly ILogger<SearchController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchController"/> class.
    /// </summary>
    /// <param name="searchService">The search service.</param>
    /// <param name="logger">The logger.</param>
    public SearchController(
        ISearchService searchService,
        ILogger<SearchController> logger)
    {
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Searches for documents within the user's tenant.
    /// </summary>
    /// <param name="request">The search request parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated search results.</returns>
    /// <response code="200">Returns the search results.</response>
    /// <response code="400">If the request is invalid.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="503">If the search service is unavailable.</response>
    [HttpPost]
    [ProducesResponseType(typeof(SearchResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Search(
        [FromBody] SearchRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get user's tenant ID from claims
            var userTenantId = User.GetTenantId();
            if (userTenantId <= 0)
            {
                return Forbid();
            }

            // Validate request
            if (request.PageSize < 1 || request.PageSize > 100)
            {
                return BadRequest(new { error = "InvalidPageSize", message = "PageSize must be between 1 and 100." });
            }

            if (request.Page < 1)
            {
                return BadRequest(new { error = "InvalidPage", message = "Page must be >= 1." });
            }

            _logger.LogInformation(
                "Searching documents for tenant {TenantId}, query: '{Query}', page: {Page}",
                userTenantId, request.Query, request.Page);

            // Build internal search request - always filter by user's tenant
            var searchRequest = new SearchRequest
            {
                Query = request.Query ?? string.Empty,
                TenantId = userTenantId, // Always enforce tenant isolation
                DocumentTypeId = request.DocumentTypeId,
                Tags = request.Tags,
                MimeType = request.MimeType,
                OnlyCurrentVersion = request.OnlyCurrentVersion,
                CreatedFrom = request.CreatedFrom,
                CreatedTo = request.CreatedTo,
                Page = request.Page,
                PageSize = request.PageSize,
                Sort = BuildSortArray(request.SortBy, request.SortOrder)
            };

            var result = await _searchService.SearchDocumentsAsync(searchRequest, cancellationToken);

            var response = new SearchResultResponse
            {
                Hits = result.Hits.Select(h => new SearchHitDto
                {
                    DocumentId = h.DocumentId,
                    TenantId = h.TenantId,
                    DocumentTypeId = h.DocumentTypeId,
                    Title = h.Title,
                    Description = h.Description,
                    OriginalFileName = h.OriginalFileName,
                    MimeType = h.MimeType,
                    Tags = h.Tags,
                    FileSizeBytes = h.FileSizeBytes ?? 0,
                    Version = h.Version,
                    IsCurrentVersion = h.IsCurrentVersion,
                    CreatedAt = h.CreatedAt,
                    CreatedBy = h.CreatedBy,
                    UpdatedAt = h.UpdatedAt
                }).ToList(),
                TotalHits = result.TotalHits,
                Page = result.Page,
                PageSize = result.PageSize,
                TotalPages = result.TotalPages,
                ProcessingTimeMs = result.ProcessingTimeMs,
                Query = result.Query
            };

            return Ok(response);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Search service is unavailable");
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "SearchUnavailable", message = "Search service is temporarily unavailable." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching documents");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "SearchError", message = "An error occurred while searching documents." });
        }
    }

    /// <summary>
    /// Gets the health status of the search service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Health status.</returns>
    [HttpGet("health")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Health(CancellationToken cancellationToken = default)
    {
        try
        {
            var isHealthy = await _searchService.IsHealthyAsync(cancellationToken);
            if (isHealthy)
            {
                return Ok(new { status = "Healthy", service = "Meilisearch" });
            }
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { status = "Unhealthy", service = "Meilisearch" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking search service health");
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { status = "Unhealthy", service = "Meilisearch", error = ex.Message });
        }
    }

    private static string[]? BuildSortArray(string? sortBy, string? sortOrder)
    {
        if (string.IsNullOrEmpty(sortBy))
        {
            return null;
        }

        var order = string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc";
        return new[] { $"{sortBy}:{order}" };
    }
}

/// <summary>
/// Search request DTO for the API.
/// </summary>
public class SearchRequestDto
{
    /// <summary>
    /// The search query text.
    /// </summary>
    public string? Query { get; set; }

    /// <summary>
    /// Filter by document type ID.
    /// </summary>
    public int? DocumentTypeId { get; set; }

    /// <summary>
    /// Filter by tags (comma-separated).
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Filter by MIME type.
    /// </summary>
    public string? MimeType { get; set; }

    /// <summary>
    /// Only return current versions (default: true).
    /// </summary>
    public bool OnlyCurrentVersion { get; set; } = true;

    /// <summary>
    /// Filter by created date from.
    /// </summary>
    public DateTime? CreatedFrom { get; set; }

    /// <summary>
    /// Filter by created date to.
    /// </summary>
    public DateTime? CreatedTo { get; set; }

    /// <summary>
    /// Page number (1-based, default: 1).
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Page size (default: 20, max: 100).
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Sort by field (e.g., "createdAt", "title", "updatedAt").
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Sort order ("asc" or "desc", default: "desc").
    /// </summary>
    public string? SortOrder { get; set; } = "desc";
}

/// <summary>
/// Search result response DTO.
/// </summary>
public class SearchResultResponse
{
    /// <summary>
    /// The search results.
    /// </summary>
    public List<SearchHitDto> Hits { get; set; } = new();

    /// <summary>
    /// Total number of matching documents.
    /// </summary>
    public int TotalHits { get; set; }

    /// <summary>
    /// Current page number.
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Results per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages.
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Processing time in milliseconds.
    /// </summary>
    public int ProcessingTimeMs { get; set; }

    /// <summary>
    /// The query that was executed.
    /// </summary>
    public string Query { get; set; } = string.Empty;
}

/// <summary>
/// Individual search hit DTO.
/// </summary>
public class SearchHitDto
{
    /// <summary>
    /// Document ID.
    /// </summary>
    public long DocumentId { get; set; }

    /// <summary>
    /// Tenant ID.
    /// </summary>
    public int TenantId { get; set; }

    /// <summary>
    /// Document type ID.
    /// </summary>
    public int DocumentTypeId { get; set; }

    /// <summary>
    /// Document title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Document description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Original file name.
    /// </summary>
    public string? OriginalFileName { get; set; }

    /// <summary>
    /// MIME type.
    /// </summary>
    public string? MimeType { get; set; }

    /// <summary>
    /// Tags as JSON string.
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Version number.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Whether this is the current version.
    /// </summary>
    public bool IsCurrentVersion { get; set; }

    /// <summary>
    /// Created timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Created by user.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Updated timestamp.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
