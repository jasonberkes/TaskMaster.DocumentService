using Meilisearch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text.Json;
using TaskMaster.DocumentService.Core.DTOs;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Search.Configuration;
using TaskMaster.DocumentService.Search.Models;

namespace TaskMaster.DocumentService.Search.Services;

/// <summary>
/// Service for Meilisearch indexing and search operations.
/// </summary>
public class MeilisearchService : IMeilisearchService
{
    private readonly MeilisearchClient _client;
    private readonly MeilisearchSettings _settings;
    private readonly ILogger<MeilisearchService> _logger;
    private bool _isInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="MeilisearchService"/> class.
    /// </summary>
    /// <param name="settings">Meilisearch settings.</param>
    /// <param name="logger">Logger instance.</param>
    public MeilisearchService(IOptions<MeilisearchSettings> settings, ILogger<MeilisearchService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _client = new MeilisearchClient(_settings.Url, _settings.ApiKey);
    }

    /// <inheritdoc/>
    public async Task InitializeIndexAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
        {
            return;
        }

        try
        {
            _logger.LogInformation("Initializing Meilisearch index: {IndexName}", _settings.IndexName);

            var index = _client.Index(_settings.IndexName);

            // Configure searchable attributes
            await index.UpdateSearchableAttributesAsync(new[]
            {
                "title",
                "description",
                "content",
                "fileName",
                "documentTypeName",
                "tags"
            });

            // Configure filterable attributes
            await index.UpdateFilterableAttributesAsync(new[]
            {
                "tenantId",
                "documentTypeId",
                "tags",
                "isArchived",
                "isCurrentVersion",
                "createdAt",
                "updatedAt"
            });

            // Configure sortable attributes
            await index.UpdateSortableAttributesAsync(new[]
            {
                "createdAt",
                "updatedAt",
                "title",
                "fileSizeBytes"
            });

            _isInitialized = true;
            _logger.LogInformation("Successfully initialized Meilisearch index: {IndexName}", _settings.IndexName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Meilisearch index: {IndexName}", _settings.IndexName);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string> IndexDocumentAsync(Document document, CancellationToken cancellationToken = default)
    {
        await InitializeIndexAsync(cancellationToken);

        try
        {
            var meilisearchDoc = MapToMeilisearchDocument(document);
            var index = _client.Index(_settings.IndexName);

            await index.AddDocumentsAsync(new[] { meilisearchDoc }, cancellationToken: cancellationToken);

            _logger.LogInformation("Indexed document {DocumentId} with Meilisearch ID {MeilisearchId}",
                document.Id, meilisearchDoc.Id);

            return meilisearchDoc.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to index document {DocumentId}", document.Id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task IndexDocumentsAsync(IEnumerable<Document> documents, CancellationToken cancellationToken = default)
    {
        await InitializeIndexAsync(cancellationToken);

        var documentList = documents.ToList();
        if (!documentList.Any())
        {
            return;
        }

        try
        {
            var meilisearchDocs = documentList.Select(MapToMeilisearchDocument).ToList();
            var index = _client.Index(_settings.IndexName);

            // Process in batches
            var batches = meilisearchDocs.Chunk(_settings.BatchSize);
            foreach (var batch in batches)
            {
                await index.AddDocumentsAsync(batch, cancellationToken: cancellationToken);
            }

            _logger.LogInformation("Indexed {Count} documents in Meilisearch", documentList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to index {Count} documents", documentList.Count);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<DocumentSearchResponse> SearchDocumentsAsync(DocumentSearchRequest request, CancellationToken cancellationToken = default)
    {
        await InitializeIndexAsync(cancellationToken);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var index = _client.Index(_settings.IndexName);

            // Build filter string
            var filters = new List<string>();

            if (request.TenantId.HasValue)
            {
                filters.Add($"tenantId = {request.TenantId.Value}");
            }

            if (request.DocumentTypeId.HasValue)
            {
                filters.Add($"documentTypeId = {request.DocumentTypeId.Value}");
            }

            if (!request.IncludeArchived)
            {
                filters.Add("isArchived = false");
            }

            if (request.Tags is { Count: > 0 })
            {
                var tagFilters = string.Join(" OR ", request.Tags.Select(t => $"tags = \"{t}\""));
                filters.Add($"({tagFilters})");
            }

            var filterString = filters.Any() ? string.Join(" AND ", filters) : null;

            // Build sort string
            var sort = new List<string>();
            if (!string.IsNullOrEmpty(request.SortBy))
            {
                var direction = request.SortDescending ? "desc" : "asc";
                sort.Add($"{request.SortBy}:{direction}");
            }

            // Execute search
            var searchQuery = new SearchQuery
            {
                Filter = filterString,
                Limit = request.PageSize,
                Offset = (request.Page - 1) * request.PageSize,
                Sort = sort.Any() ? sort.ToArray() : null
            };

            var searchResult = await index.SearchAsync<MeilisearchDocument>(
                request.Query,
                searchQuery,
                cancellationToken);

            stopwatch.Stop();

            // Map results
            var results = searchResult.Hits.Select(hit => new DocumentSearchResult
            {
                Document = MapToDocumentDto(hit),
                Score = 1.0, // Meilisearch doesn't expose scores in the same way
                Highlights = ExtractHighlights(hit)
            }).ToList();

            return new DocumentSearchResponse
            {
                Results = results,
                TotalCount = searchResult.Hits.Count(),
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling(searchResult.Hits.Count() / (double)request.PageSize),
                ProcessingTimeMs = searchResult.ProcessingTimeMs,
                Query = request.Query
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search documents with query: {Query}", request.Query);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task RemoveDocumentAsync(string meilisearchId, CancellationToken cancellationToken = default)
    {
        try
        {
            var index = _client.Index(_settings.IndexName);
            await index.DeleteOneDocumentAsync(meilisearchId, cancellationToken);

            _logger.LogInformation("Removed document {MeilisearchId} from Meilisearch", meilisearchId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove document {MeilisearchId} from Meilisearch", meilisearchId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task RemoveDocumentsAsync(IEnumerable<string> meilisearchIds, CancellationToken cancellationToken = default)
    {
        var idList = meilisearchIds.ToList();
        if (!idList.Any())
        {
            return;
        }

        try
        {
            var index = _client.Index(_settings.IndexName);
            await index.DeleteDocumentsAsync(idList, cancellationToken);

            _logger.LogInformation("Removed {Count} documents from Meilisearch", idList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove {Count} documents from Meilisearch", idList.Count);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
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

    private MeilisearchDocument MapToMeilisearchDocument(Document document)
    {
        List<string>? tags = null;
        if (!string.IsNullOrEmpty(document.Tags))
        {
            try
            {
                tags = JsonSerializer.Deserialize<List<string>>(document.Tags);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize tags for document {DocumentId}", document.Id);
            }
        }

        return new MeilisearchDocument
        {
            Id = $"{document.TenantId}_{document.Id}",
            DocumentId = document.Id,
            TenantId = document.TenantId,
            DocumentTypeId = document.DocumentTypeId,
            DocumentTypeName = document.DocumentType?.Name ?? string.Empty,
            Title = document.Title,
            Description = document.Description,
            Content = document.ExtractedText,
            FileName = document.OriginalFileName,
            MimeType = document.MimeType,
            FileSizeBytes = document.FileSizeBytes,
            Tags = tags ?? new List<string>(),
            Version = document.Version,
            IsCurrentVersion = document.IsCurrentVersion,
            CreatedAt = new DateTimeOffset(document.CreatedAt).ToUnixTimeSeconds(),
            CreatedBy = document.CreatedBy,
            UpdatedAt = document.UpdatedAt.HasValue
                ? new DateTimeOffset(document.UpdatedAt.Value).ToUnixTimeSeconds()
                : null,
            IsArchived = document.IsArchived
        };
    }

    private static DocumentDto MapToDocumentDto(MeilisearchDocument doc)
    {
        return new DocumentDto
        {
            Id = doc.DocumentId,
            TenantId = doc.TenantId,
            DocumentTypeId = doc.DocumentTypeId,
            DocumentTypeName = doc.DocumentTypeName,
            Title = doc.Title,
            Description = doc.Description,
            OriginalFileName = doc.FileName,
            MimeType = doc.MimeType,
            FileSizeBytes = doc.FileSizeBytes,
            Tags = doc.Tags,
            Version = doc.Version,
            IsCurrentVersion = doc.IsCurrentVersion,
            CreatedAt = DateTimeOffset.FromUnixTimeSeconds(doc.CreatedAt).DateTime,
            CreatedBy = doc.CreatedBy,
            UpdatedAt = doc.UpdatedAt.HasValue
                ? DateTimeOffset.FromUnixTimeSeconds(doc.UpdatedAt.Value).DateTime
                : null,
            IsArchived = doc.IsArchived
        };
    }

    private static List<string> ExtractHighlights(MeilisearchDocument doc)
    {
        var highlights = new List<string>();

        // Add relevant text snippets
        if (!string.IsNullOrEmpty(doc.Description))
        {
            highlights.Add(doc.Description.Length > 200
                ? doc.Description.Substring(0, 200) + "..."
                : doc.Description);
        }

        if (!string.IsNullOrEmpty(doc.Content))
        {
            highlights.Add(doc.Content.Length > 300
                ? doc.Content.Substring(0, 300) + "..."
                : doc.Content);
        }

        return highlights;
    }
}
