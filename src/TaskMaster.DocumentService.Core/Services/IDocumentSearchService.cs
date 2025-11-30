using TaskMaster.DocumentService.Core.DTOs;

namespace TaskMaster.DocumentService.Core.Services;

/// <summary>
/// Service interface for document search operations.
/// </summary>
public interface IDocumentSearchService
{
    /// <summary>
    /// Searches for documents using full-text search.
    /// </summary>
    /// <param name="request">The search request parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The search results.</returns>
    Task<DocumentSearchResponse> SearchAsync(DocumentSearchRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Indexes a document for search.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task IndexDocumentAsync(long documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reindexes all documents for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of documents indexed.</returns>
    Task<int> ReindexTenantDocumentsAsync(int tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a document from the search index.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveFromIndexAsync(long documentId, CancellationToken cancellationToken = default);
}
