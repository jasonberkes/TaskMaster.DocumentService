using TaskMaster.DocumentService.Core.DTOs;
using TaskMaster.DocumentService.Core.Entities;

namespace TaskMaster.DocumentService.Search.Services;

/// <summary>
/// Interface for Meilisearch indexing and search operations.
/// </summary>
public interface IMeilisearchService
{
    /// <summary>
    /// Initializes the Meilisearch index with the appropriate settings.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InitializeIndexAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Indexes a single document in Meilisearch.
    /// </summary>
    /// <param name="document">The document to index.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The Meilisearch document ID.</returns>
    Task<string> IndexDocumentAsync(Document document, CancellationToken cancellationToken = default);

    /// <summary>
    /// Indexes multiple documents in Meilisearch.
    /// </summary>
    /// <param name="documents">The documents to index.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task IndexDocumentsAsync(IEnumerable<Document> documents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for documents using Meilisearch full-text search.
    /// </summary>
    /// <param name="request">The search request parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The search results.</returns>
    Task<DocumentSearchResponse> SearchDocumentsAsync(DocumentSearchRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a document from the Meilisearch index.
    /// </summary>
    /// <param name="meilisearchId">The Meilisearch document ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveDocumentAsync(string meilisearchId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes multiple documents from the Meilisearch index.
    /// </summary>
    /// <param name="meilisearchIds">The Meilisearch document IDs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveDocumentsAsync(IEnumerable<string> meilisearchIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks the health of the Meilisearch service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the service is healthy; otherwise, false.</returns>
    Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);
}
