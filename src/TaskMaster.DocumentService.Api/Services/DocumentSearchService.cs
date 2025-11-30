using Microsoft.Extensions.Logging;
using TaskMaster.DocumentService.Core.DTOs;
using TaskMaster.DocumentService.Core.Services;
using TaskMaster.DocumentService.Data.Repositories;
using TaskMaster.DocumentService.Search.Services;

namespace TaskMaster.DocumentService.Api.Services;

/// <summary>
/// Service for document search operations that coordinates repository and Meilisearch.
/// </summary>
public class DocumentSearchService : IDocumentSearchService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IMeilisearchService _meilisearchService;
    private readonly ILogger<DocumentSearchService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentSearchService"/> class.
    /// </summary>
    /// <param name="documentRepository">Document repository.</param>
    /// <param name="meilisearchService">Meilisearch service.</param>
    /// <param name="logger">Logger instance.</param>
    public DocumentSearchService(
        IDocumentRepository documentRepository,
        IMeilisearchService meilisearchService,
        ILogger<DocumentSearchService> logger)
    {
        _documentRepository = documentRepository;
        _meilisearchService = meilisearchService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<DocumentSearchResponse> SearchAsync(DocumentSearchRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Searching documents with query: {Query}", request.Query);

            var results = await _meilisearchService.SearchDocumentsAsync(request, cancellationToken);

            _logger.LogInformation("Found {Count} documents for query: {Query}", results.TotalCount, request.Query);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching documents with query: {Query}", request.Query);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task IndexDocumentAsync(long documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Indexing document {DocumentId}", documentId);

            var document = await _documentRepository.GetByIdAsync(documentId, cancellationToken);
            if (document == null)
            {
                _logger.LogWarning("Document {DocumentId} not found for indexing", documentId);
                throw new InvalidOperationException($"Document {documentId} not found");
            }

            // Only index if the document type is configured for indexing
            if (!document.DocumentType.IsContentIndexed)
            {
                _logger.LogInformation("Document type {DocumentType} is not configured for indexing",
                    document.DocumentType.Name);
                return;
            }

            var meilisearchId = await _meilisearchService.IndexDocumentAsync(document, cancellationToken);
            await _documentRepository.UpdateIndexingInfoAsync(documentId, meilisearchId, cancellationToken);

            _logger.LogInformation("Successfully indexed document {DocumentId}", documentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing document {DocumentId}", documentId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<int> ReindexTenantDocumentsAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Reindexing documents for tenant {TenantId}", tenantId);

            var documents = await _documentRepository.GetByTenantIdAsync(tenantId, includeArchived: true, cancellationToken);

            // Filter to only documents that should be indexed
            var documentsToIndex = documents.Where(d => d.DocumentType.IsContentIndexed).ToList();

            if (!documentsToIndex.Any())
            {
                _logger.LogInformation("No documents to reindex for tenant {TenantId}", tenantId);
                return 0;
            }

            await _meilisearchService.IndexDocumentsAsync(documentsToIndex, cancellationToken);

            // Update indexing info for all documents
            foreach (var document in documentsToIndex)
            {
                var meilisearchId = $"{document.TenantId}_{document.Id}";
                await _documentRepository.UpdateIndexingInfoAsync(document.Id, meilisearchId, cancellationToken);
            }

            _logger.LogInformation("Successfully reindexed {Count} documents for tenant {TenantId}",
                documentsToIndex.Count, tenantId);

            return documentsToIndex.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reindexing documents for tenant {TenantId}", tenantId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task RemoveFromIndexAsync(long documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Removing document {DocumentId} from search index", documentId);

            var document = await _documentRepository.GetByIdAsync(documentId, cancellationToken);
            if (document?.MeilisearchId != null)
            {
                await _meilisearchService.RemoveDocumentAsync(document.MeilisearchId, cancellationToken);
                _logger.LogInformation("Successfully removed document {DocumentId} from search index", documentId);
            }
            else
            {
                _logger.LogWarning("Document {DocumentId} not found or not indexed", documentId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing document {DocumentId} from search index", documentId);
            throw;
        }
    }
}
