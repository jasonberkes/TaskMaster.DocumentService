using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Search.Models;

namespace TaskMaster.DocumentService.Search.Interfaces;

/// <summary>
/// Defines the contract for document search operations using Meilisearch.
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// Initializes the search index with proper configuration.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InitializeIndexAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Indexes a single document in Meilisearch.
    /// </summary>
    /// <param name="document">The document to index.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The Meilisearch document identifier.</returns>
    Task<string> IndexDocumentAsync(Document document, CancellationToken cancellationToken = default);

    /// <summary>
    /// Indexes multiple documents in Meilisearch in batch.
    /// </summary>
    /// <param name="documents">The documents to index.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary mapping document IDs to their Meilisearch identifiers.</returns>
    Task<Dictionary<long, string>> IndexDocumentsBatchAsync(IEnumerable<Document> documents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a document in the search index.
    /// </summary>
    /// <param name="document">The document to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateDocumentAsync(Document document, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a document from the search index.
    /// </summary>
    /// <param name="meilisearchId">The Meilisearch document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveDocumentAsync(string meilisearchId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes multiple documents from the search index.
    /// </summary>
    /// <param name="meilisearchIds">The Meilisearch document identifiers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveDocumentsBatchAsync(IEnumerable<string> meilisearchIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for documents using the provided search request.
    /// </summary>
    /// <param name="searchRequest">The search request parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The search results.</returns>
    Task<SearchResult<SearchableDocument>> SearchDocumentsAsync(SearchRequest searchRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the health status of the Meilisearch service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the service is healthy, otherwise false.</returns>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all documents from the search index.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ClearIndexAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Indexes blob metadata entries in the search index.
    /// </summary>
    /// <param name="blobMetadataList">The blob metadata entries to index.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task IndexBlobMetadataAsync(IEnumerable<BlobMetadata> blobMetadataList, CancellationToken cancellationToken = default);
}
