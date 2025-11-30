using TaskMaster.DocumentService.Core.Entities;

namespace TaskMaster.DocumentService.Data.Repositories;

/// <summary>
/// Repository interface for document operations.
/// </summary>
public interface IDocumentRepository
{
    /// <summary>
    /// Gets a document by its identifier.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The document if found; otherwise, null.</returns>
    Task<Document?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets documents by tenant identifier.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="includeArchived">Whether to include archived documents.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The list of documents.</returns>
    Task<List<Document>> GetByTenantIdAsync(int tenantId, bool includeArchived = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets documents by document type identifier.
    /// </summary>
    /// <param name="documentTypeId">The document type identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The list of documents.</returns>
    Task<List<Document>> GetByDocumentTypeIdAsync(int documentTypeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all documents that need to be indexed.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The list of documents.</returns>
    Task<List<Document>> GetUnindexedDocumentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new document.
    /// </summary>
    /// <param name="document">The document to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created document.</returns>
    Task<Document> CreateAsync(Document document, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing document.
    /// </summary>
    /// <param name="document">The document to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(Document document, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document (soft delete).
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="deletedBy">The user performing the deletion.</param>
    /// <param name="reason">The reason for deletion.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the document was deleted; otherwise, false.</returns>
    Task<bool> DeleteAsync(long id, string deletedBy, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the Meilisearch indexing information for a document.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="meilisearchId">The Meilisearch document ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateIndexingInfoAsync(long documentId, string meilisearchId, CancellationToken cancellationToken = default);
}
