using Microsoft.Extensions.Logging;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Interfaces;
using TaskMaster.DocumentService.Search.Interfaces;

namespace TaskMaster.DocumentService.Search.Services;

/// <summary>
/// Implementation of IDocumentIndexer that wraps the search service.
/// Provides a clean abstraction for the Core layer to trigger indexing.
/// </summary>
public class DocumentIndexer : IDocumentIndexer
{
    private readonly ISearchService _searchService;
    private readonly ILogger<DocumentIndexer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentIndexer"/> class.
    /// </summary>
    /// <param name="searchService">The search service.</param>
    /// <param name="logger">The logger instance.</param>
    public DocumentIndexer(
        ISearchService searchService,
        ILogger<DocumentIndexer> logger)
    {
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<string?> IndexDocumentAsync(Document document, CancellationToken cancellationToken = default)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        try
        {
            _logger.LogInformation("Indexing document {DocumentId} '{Title}'", document.Id, document.Title);
            
            var meilisearchId = await _searchService.IndexDocumentAsync(document, cancellationToken);
            
            _logger.LogInformation(
                "Successfully indexed document {DocumentId} with MeilisearchId {MeilisearchId}",
                document.Id, meilisearchId);
            
            return meilisearchId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to index document {DocumentId} '{Title}'", document.Id, document.Title);
            // Return null instead of throwing - indexing failure shouldn't block document creation
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<Dictionary<long, string>> IndexDocumentsBatchAsync(
        IEnumerable<Document> documents, 
        CancellationToken cancellationToken = default)
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
            _logger.LogInformation("Batch indexing {Count} documents", documentList.Count);
            
            var result = await _searchService.IndexDocumentsBatchAsync(documentList, cancellationToken);
            
            _logger.LogInformation("Successfully batch indexed {Count} documents", result.Count);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to batch index {Count} documents", documentList.Count);
            return new Dictionary<long, string>();
        }
    }

    /// <inheritdoc/>
    public async Task RemoveDocumentAsync(string meilisearchId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(meilisearchId))
        {
            return;
        }

        try
        {
            _logger.LogInformation("Removing document from index: {MeilisearchId}", meilisearchId);
            
            await _searchService.RemoveDocumentAsync(meilisearchId, cancellationToken);
            
            _logger.LogInformation("Successfully removed document from index: {MeilisearchId}", meilisearchId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove document from index: {MeilisearchId}", meilisearchId);
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
            _logger.LogInformation("Updating document in index: {DocumentId}", document.Id);
            
            await _searchService.UpdateDocumentAsync(document, cancellationToken);
            
            _logger.LogInformation("Successfully updated document in index: {DocumentId}", document.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update document in index: {DocumentId}", document.Id);
        }
    }
}
