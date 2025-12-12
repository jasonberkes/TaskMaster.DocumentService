using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskMaster.DocumentService.Core.Interfaces;
using TaskMaster.DocumentService.Search.Interfaces;

namespace TaskMaster.DocumentService.Api.Controllers;

/// <summary>
/// Administrative endpoints for system maintenance operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // Requires authentication
public class AdminController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISearchService _searchService;
    private readonly ILogger<AdminController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminController"/> class.
    /// </summary>
    /// <param name="unitOfWork">The unit of work.</param>
    /// <param name="searchService">The search service.</param>
    /// <param name="logger">The logger.</param>
    public AdminController(
        IUnitOfWork unitOfWork,
        ISearchService searchService,
        ILogger<AdminController> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Reindexes all documents or documents needing indexing.
    /// </summary>
    /// <param name="fullReindex">If true, reindex all documents. If false, only index documents that need it.</param>
    /// <param name="tenantId">Optional tenant ID to limit reindexing to a specific tenant.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The reindex operation result.</returns>
    [HttpPost("reindex")]
    [ProducesResponseType(typeof(ReindexResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ReindexDocuments(
        [FromQuery] bool fullReindex = false,
        [FromQuery] int? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Starting reindex operation. FullReindex: {FullReindex}, TenantId: {TenantId}",
                fullReindex, tenantId);

            var startTime = DateTime.UtcNow;
            int totalDocuments;
            int indexedCount = 0;
            int failedCount = 0;

            if (fullReindex)
            {
                // Clear the index first
                await _searchService.ClearIndexAsync(cancellationToken);
                await _searchService.InitializeIndexAsync(cancellationToken);

                // Get all documents (optionally filtered by tenant)
                IEnumerable<Core.Entities.Document> documents;
                if (tenantId.HasValue)
                {
                    documents = await _unitOfWork.Documents.GetByTenantIdAsync(tenantId.Value, false, cancellationToken);
                }
                else
                {
                    documents = await _unitOfWork.Documents.GetAllAsync(cancellationToken);
                }

                var documentList = documents
                    .Where(d => !d.IsDeleted && d.IsCurrentVersion)
                    .ToList();
                totalDocuments = documentList.Count;

                // Index in batches
                const int batchSize = 100;
                for (var i = 0; i < documentList.Count; i += batchSize)
                {
                    var batch = documentList.Skip(i).Take(batchSize).ToList();

                    try
                    {
                        var results = await _searchService.IndexDocumentsBatchAsync(batch, cancellationToken);

                        foreach (var document in batch)
                        {
                            if (results.TryGetValue(document.Id, out var meilisearchId))
                            {
                                document.MeilisearchId = meilisearchId;
                                document.LastIndexedAt = DateTime.UtcNow;
                                _unitOfWork.Documents.Update(document);
                                indexedCount++;
                            }
                            else
                            {
                                failedCount++;
                            }
                        }

                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                    }
                    catch (Exception batchEx)
                    {
                        _logger.LogError(batchEx, "Failed to index batch starting at index {Index}", i);
                        failedCount += batch.Count;
                    }
                }
            }
            else
            {
                // Only reindex documents that need it
                var documents = (await _unitOfWork.Documents.GetDocumentsNeedingIndexingAsync(cancellationToken))
                    .ToList();

                if (tenantId.HasValue)
                {
                    documents = documents.Where(d => d.TenantId == tenantId.Value).ToList();
                }

                totalDocuments = documents.Count;

                const int batchSize = 100;
                for (var i = 0; i < documents.Count; i += batchSize)
                {
                    var batch = documents.Skip(i).Take(batchSize).ToList();

                    try
                    {
                        var results = await _searchService.IndexDocumentsBatchAsync(batch, cancellationToken);

                        foreach (var document in batch)
                        {
                            if (results.TryGetValue(document.Id, out var meilisearchId))
                            {
                                document.MeilisearchId = meilisearchId;
                                document.LastIndexedAt = DateTime.UtcNow;
                                _unitOfWork.Documents.Update(document);
                                indexedCount++;
                            }
                            else
                            {
                                failedCount++;
                            }
                        }

                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                    }
                    catch (Exception batchEx)
                    {
                        _logger.LogError(batchEx, "Failed to index batch starting at index {Index}", i);
                        failedCount += batch.Count;
                    }
                }
            }

            var duration = DateTime.UtcNow - startTime;

            var result = new ReindexResult
            {
                TotalDocuments = totalDocuments,
                IndexedCount = indexedCount,
                FailedCount = failedCount,
                DurationSeconds = duration.TotalSeconds,
                FullReindex = fullReindex,
                TenantId = tenantId,
                CompletedAt = DateTime.UtcNow
            };

            _logger.LogInformation(
                "Reindex operation completed. Total: {Total}, Indexed: {Indexed}, Failed: {Failed}, Duration: {Duration}s",
                totalDocuments, indexedCount, failedCount, duration.TotalSeconds);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reindex operation failed");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "ReindexFailed",
                message = "An error occurred during the reindex operation."
            });
        }
    }

    /// <summary>
    /// Gets the current search index statistics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Index statistics.</returns>
    [HttpGet("index-stats")]
    [ProducesResponseType(typeof(IndexStats), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetIndexStats(CancellationToken cancellationToken = default)
    {
        try
        {
            var totalDocuments = await _unitOfWork.Documents.CountAsync(d => !d.IsDeleted && d.IsCurrentVersion, cancellationToken);
            var indexedDocuments = await _unitOfWork.Documents.CountAsync(d => !d.IsDeleted && d.IsCurrentVersion && d.LastIndexedAt != null, cancellationToken);
            var needsIndexing = await _unitOfWork.Documents.CountAsync(d => !d.IsDeleted && d.IsCurrentVersion && (d.LastIndexedAt == null || d.UpdatedAt > d.LastIndexedAt), cancellationToken);

            var isHealthy = await _searchService.IsHealthyAsync(cancellationToken);

            return Ok(new IndexStats
            {
                TotalDocuments = totalDocuments,
                IndexedDocuments = indexedDocuments,
                DocumentsNeedingIndexing = needsIndexing,
                SearchServiceHealthy = isHealthy,
                RetrievedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get index statistics");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "StatsFailed",
                message = "Failed to retrieve index statistics."
            });
        }
    }

    /// <summary>
    /// Clears the entire search index.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Clear operation result.</returns>
    [HttpDelete("clear-index")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ClearIndex(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogWarning("Clearing search index");

            await _searchService.ClearIndexAsync(cancellationToken);

            // Clear MeilisearchId and LastIndexedAt from all documents
            var documents = (await _unitOfWork.Documents.GetAllAsync(cancellationToken)).ToList();
            foreach (var document in documents)
            {
                document.MeilisearchId = null;
                document.LastIndexedAt = null;
                _unitOfWork.Documents.Update(document);
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Search index cleared. {Count} document records updated", documents.Count);

            return Ok(new
            {
                message = "Search index cleared successfully",
                documentsUpdated = documents.Count,
                clearedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear search index");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "ClearIndexFailed",
                message = "Failed to clear search index."
            });
        }
    }
}

/// <summary>
/// Result of a reindex operation.
/// </summary>
public class ReindexResult
{
    /// <summary>
    /// Total number of documents processed.
    /// </summary>
    public int TotalDocuments { get; set; }

    /// <summary>
    /// Number of documents successfully indexed.
    /// </summary>
    public int IndexedCount { get; set; }

    /// <summary>
    /// Number of documents that failed to index.
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// Duration of the operation in seconds.
    /// </summary>
    public double DurationSeconds { get; set; }

    /// <summary>
    /// Whether this was a full reindex.
    /// </summary>
    public bool FullReindex { get; set; }

    /// <summary>
    /// Tenant ID if filtered.
    /// </summary>
    public int? TenantId { get; set; }

    /// <summary>
    /// When the operation completed.
    /// </summary>
    public DateTime CompletedAt { get; set; }
}

/// <summary>
/// Search index statistics.
/// </summary>
public class IndexStats
{
    /// <summary>
    /// Total documents in the system.
    /// </summary>
    public int TotalDocuments { get; set; }

    /// <summary>
    /// Documents that have been indexed.
    /// </summary>
    public int IndexedDocuments { get; set; }

    /// <summary>
    /// Documents that need indexing.
    /// </summary>
    public int DocumentsNeedingIndexing { get; set; }

    /// <summary>
    /// Whether the search service is healthy.
    /// </summary>
    public bool SearchServiceHealthy { get; set; }

    /// <summary>
    /// When these stats were retrieved.
    /// </summary>
    public DateTime RetrievedAt { get; set; }
}
