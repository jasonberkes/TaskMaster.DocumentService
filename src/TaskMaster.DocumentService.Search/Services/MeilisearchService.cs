using Meilisearch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Search.Configuration;
using TaskMaster.DocumentService.Search.Interfaces;
using TaskMaster.DocumentService.Search.Models;

namespace TaskMaster.DocumentService.Search.Services;

/// <summary>
/// Implementation of the search service using Meilisearch for document indexing and search.
/// </summary>
public class MeilisearchService : ISearchService
{
    private readonly MeilisearchClient _client;
    private readonly MeilisearchOptions _options;
    private readonly ILogger<MeilisearchService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MeilisearchService"/> class.
    /// </summary>
    /// <param name="options">The Meilisearch configuration options.</param>
    /// <param name="logger">The logger instance.</param>
    public MeilisearchService(
        IOptions<MeilisearchOptions> options,
        ILogger<MeilisearchService> logger)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options.Value ?? throw new InvalidOperationException("MeilisearchOptions value cannot be null");

        _client = new MeilisearchClient(_options.Url, _options.ApiKey);
    }

    /// <inheritdoc/>
    public async Task InitializeIndexAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Initializing Meilisearch index: {IndexName}", _options.IndexName);

            // Ensure index exists with correct primary key (lowercase 'id' to match JSON serialization)
            try
            {
                await _client.CreateIndexAsync(_options.IndexName, "id");
                _logger.LogInformation("Created Meilisearch index: {IndexName}", _options.IndexName);
            }
            catch (Meilisearch.MeilisearchApiError ex) when (ex.Message.Contains("already exists"))
            {
                _logger.LogDebug("Meilisearch index {IndexName} already exists", _options.IndexName);
            }

            var index = _client.Index(_options.IndexName);
            // Configure searchable attributes (fields that can be searched)
            await index.UpdateSearchableAttributesAsync(new[]
            {
                nameof(SearchableDocument.Title),
                nameof(SearchableDocument.Description),
                nameof(SearchableDocument.ExtractedText),
                nameof(SearchableDocument.OriginalFileName),
                nameof(SearchableDocument.Tags),
                nameof(SearchableDocument.Metadata)
            });

            // Configure filterable attributes (fields that can be used in filters)
            await index.UpdateFilterableAttributesAsync(new[]
            {
                nameof(SearchableDocument.TenantId),
                nameof(SearchableDocument.DocumentTypeId),
                nameof(SearchableDocument.MimeType),
                nameof(SearchableDocument.IsCurrentVersion),
                nameof(SearchableDocument.CreatedAt),
                nameof(SearchableDocument.UpdatedAt),
                nameof(SearchableDocument.Tags)
            });

            // Configure sortable attributes
            await index.UpdateSortableAttributesAsync(new[]
            {
                nameof(SearchableDocument.CreatedAt),
                nameof(SearchableDocument.UpdatedAt),
                nameof(SearchableDocument.Title),
                nameof(SearchableDocument.FileSizeBytes)
            });

            // Configure displayed attributes (fields returned in search results)
            await index.UpdateDisplayedAttributesAsync(new[]
            {
                nameof(SearchableDocument.Id),
                nameof(SearchableDocument.DocumentId),
                nameof(SearchableDocument.TenantId),
                nameof(SearchableDocument.DocumentTypeId),
                nameof(SearchableDocument.Title),
                nameof(SearchableDocument.Description),
                nameof(SearchableDocument.OriginalFileName),
                nameof(SearchableDocument.MimeType),
                nameof(SearchableDocument.Tags),
                nameof(SearchableDocument.FileSizeBytes),
                nameof(SearchableDocument.Version),
                nameof(SearchableDocument.IsCurrentVersion),
                nameof(SearchableDocument.CreatedAt),
                nameof(SearchableDocument.CreatedBy),
                nameof(SearchableDocument.UpdatedAt),
                nameof(SearchableDocument.UpdatedBy)
            });

            _logger.LogInformation("Successfully initialized Meilisearch index: {IndexName}", _options.IndexName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Meilisearch index: {IndexName}", _options.IndexName);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string> IndexDocumentAsync(Document document, CancellationToken cancellationToken = default)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        try
        {
            var searchableDoc = MapToSearchableDocument(document);
            var index = _client.Index(_options.IndexName);

            await index.AddDocumentsAsync(new[] { searchableDoc }, primaryKey: "id", cancellationToken);

            _logger.LogInformation("Successfully indexed document {DocumentId} with Meilisearch ID {MeilisearchId}",
                document.Id, searchableDoc.Id);

            return searchableDoc.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to index document {DocumentId}", document.Id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Dictionary<long, string>> IndexDocumentsBatchAsync(IEnumerable<Document> documents, CancellationToken cancellationToken = default)
    {
        if (documents == null)
        {
            throw new ArgumentNullException(nameof(documents));
        }

        var documentList = documents.ToList();
        if (!documentList.Any())
        {
            return new Dictionary<long, string>();
        }

        try
        {
            var searchableDocs = documentList.Select(MapToSearchableDocument).ToList();
            var index = _client.Index(_options.IndexName);

            // Process in batches
            var batches = searchableDocs
                .Select((doc, idx) => new { doc, idx })
                .GroupBy(x => x.idx / _options.BatchSize)
                .Select(g => g.Select(x => x.doc).ToList());

            var result = new Dictionary<long, string>();

            foreach (var batch in batches)
            {
                await index.AddDocumentsAsync(batch, primaryKey: "id", cancellationToken);

                foreach (var doc in batch)
                {
                    result[doc.DocumentId] = doc.Id;
                }
            }

            _logger.LogInformation("Successfully indexed {Count} documents in batch", documentList.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to index documents batch");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task UpdateDocumentAsync(Document document, CancellationToken cancellationToken = default)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        try
        {
            var searchableDoc = MapToSearchableDocument(document);
            var index = _client.Index(_options.IndexName);

            await index.UpdateDocumentsAsync(new[] { searchableDoc }, primaryKey: "id", cancellationToken);

            _logger.LogInformation("Successfully updated document {DocumentId} in Meilisearch", document.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update document {DocumentId}", document.Id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task RemoveDocumentAsync(string meilisearchId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(meilisearchId))
        {
            throw new ArgumentException("Meilisearch ID cannot be null or empty", nameof(meilisearchId));
        }

        try
        {
            var index = _client.Index(_options.IndexName);
            await index.DeleteOneDocumentAsync(meilisearchId, cancellationToken);

            _logger.LogInformation("Successfully removed document {MeilisearchId} from Meilisearch", meilisearchId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove document {MeilisearchId}", meilisearchId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task RemoveDocumentsBatchAsync(IEnumerable<string> meilisearchIds, CancellationToken cancellationToken = default)
    {
        if (meilisearchIds == null)
        {
            throw new ArgumentNullException(nameof(meilisearchIds));
        }

        var idList = meilisearchIds.ToList();
        if (!idList.Any())
        {
            return;
        }

        try
        {
            var index = _client.Index(_options.IndexName);
            await index.DeleteDocumentsAsync(idList, cancellationToken);

            _logger.LogInformation("Successfully removed {Count} documents from Meilisearch", idList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove documents batch");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Models.SearchResult<SearchableDocument>> SearchDocumentsAsync(SearchRequest searchRequest, CancellationToken cancellationToken = default)
    {
        if (searchRequest == null)
        {
            throw new ArgumentNullException(nameof(searchRequest));
        }

        try
        {
            var index = _client.Index(_options.IndexName);
            var query = new SearchQuery
            {
                Offset = (searchRequest.Page - 1) * searchRequest.PageSize,
                Limit = searchRequest.PageSize,
                AttributesToSearchOn = searchRequest.SearchableAttributes,
                Sort = searchRequest.Sort
            };

            // Build filters
            var filters = new List<string>();

            if (searchRequest.TenantId.HasValue)
            {
                filters.Add($"{nameof(SearchableDocument.TenantId)} = {searchRequest.TenantId.Value}");
            }

            if (searchRequest.DocumentTypeId.HasValue)
            {
                filters.Add($"{nameof(SearchableDocument.DocumentTypeId)} = {searchRequest.DocumentTypeId.Value}");
            }

            if (!string.IsNullOrEmpty(searchRequest.MimeType))
            {
                filters.Add($"{nameof(SearchableDocument.MimeType)} = \"{searchRequest.MimeType}\"");
            }

            if (searchRequest.OnlyCurrentVersion)
            {
                filters.Add($"{nameof(SearchableDocument.IsCurrentVersion)} = true");
            }

            if (searchRequest.CreatedFrom.HasValue)
            {
                var timestamp = new DateTimeOffset(searchRequest.CreatedFrom.Value).ToUnixTimeSeconds();
                filters.Add($"{nameof(SearchableDocument.CreatedAt)} >= {timestamp}");
            }

            if (searchRequest.CreatedTo.HasValue)
            {
                var timestamp = new DateTimeOffset(searchRequest.CreatedTo.Value).ToUnixTimeSeconds();
                filters.Add($"{nameof(SearchableDocument.CreatedAt)} <= {timestamp}");
            }

            if (!string.IsNullOrEmpty(searchRequest.Tags))
            {
                filters.Add($"{nameof(SearchableDocument.Tags)} = \"{searchRequest.Tags}\"");
            }

            if (filters.Any())
            {
                query.Filter = string.Join(" AND ", filters);
            }

            var response = await index.SearchAsync<SearchableDocument>(
                searchRequest.Query,
                query,
                cancellationToken);

            var hits = response.Hits?.ToList() ?? new List<SearchableDocument>();
            var totalHits = hits.Count; // For this version of Meilisearch, use actual hit count
            var totalPages = (int)Math.Ceiling((double)totalHits / searchRequest.PageSize);

            _logger.LogInformation("Search completed: Query='{Query}', Hits={Hits}, ProcessingTime={ProcessingTime}ms",
                searchRequest.Query, totalHits, response.ProcessingTimeMs);

            return new Models.SearchResult<SearchableDocument>
            {
                Hits = hits,
                TotalHits = totalHits,
                PageSize = searchRequest.PageSize,
                Page = searchRequest.Page,
                TotalPages = totalPages,
                ProcessingTimeMs = response.ProcessingTimeMs,
                Query = searchRequest.Query
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search documents with query: {Query}", searchRequest.Query);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var health = await _client.HealthAsync(cancellationToken);
            return health.Status == "available";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Meilisearch health check failed");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task ClearIndexAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var index = _client.Index(_options.IndexName);
            await index.DeleteAllDocumentsAsync(cancellationToken);

            _logger.LogInformation("Successfully cleared Meilisearch index: {IndexName}", _options.IndexName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear Meilisearch index: {IndexName}", _options.IndexName);
            throw;
        }
    }

    /// <summary>
    /// Maps a Document entity to a SearchableDocument.
    /// </summary>
    /// <param name="document">The document to map.</param>
    /// <returns>The searchable document.</returns>
    private SearchableDocument MapToSearchableDocument(Document document)
    {
        // Generate a unique Meilisearch ID if not already set
        var meilisearchId = !string.IsNullOrEmpty(document.MeilisearchId)
            ? document.MeilisearchId
            : $"doc_{document.Id}_{Guid.NewGuid():N}";

        return new SearchableDocument
        {
            Id = meilisearchId,
            DocumentId = document.Id,
            TenantId = document.TenantId,
            DocumentTypeId = document.DocumentTypeId,
            Title = document.Title,
            Description = document.Description,
            ExtractedText = document.ExtractedText,
            OriginalFileName = document.OriginalFileName,
            MimeType = document.MimeType,
            Tags = document.Tags,
            Metadata = document.Metadata,
            FileSizeBytes = document.FileSizeBytes,
            Version = document.Version,
            IsCurrentVersion = document.IsCurrentVersion,
            CreatedAt = document.CreatedAt,
            CreatedBy = document.CreatedBy,
            UpdatedAt = document.UpdatedAt,
            UpdatedBy = document.UpdatedBy
        };
    }
}
