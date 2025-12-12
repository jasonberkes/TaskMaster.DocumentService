using TaskMaster.DocumentService.Core.Entities;

namespace TaskMaster.DocumentService.Core.Interfaces;

/// <summary>
/// Interface for document indexing operations.
/// This abstraction allows the Core layer to trigger indexing without depending on Search implementation.
/// </summary>
public interface IDocumentIndexer
{
    /// <summary>
    /// Indexes a document for full-text search.
    /// </summary>
    /// <param name="document">The document to index.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The search index identifier, or null if indexing failed.</returns>
    Task<string?> IndexDocumentAsync(Document document, CancellationToken cancellationToken = default);

    /// <summary>
    /// Indexes multiple documents in batch.
    /// </summary>
    /// <param name="documents">The documents to index.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary mapping document IDs to search index identifiers.</returns>
    Task<Dictionary<long, string>> IndexDocumentsBatchAsync(IEnumerable<Document> documents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a document from the search index.
    /// </summary>
    /// <param name="meilisearchId">The search index identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveDocumentAsync(string meilisearchId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a document in the search index.
    /// </summary>
    /// <param name="document">The document to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateDocumentAsync(Document document, CancellationToken cancellationToken = default);
}
