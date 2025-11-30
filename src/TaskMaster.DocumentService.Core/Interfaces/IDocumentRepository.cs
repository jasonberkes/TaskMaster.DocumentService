using TaskMaster.DocumentService.Core.Entities;

namespace TaskMaster.DocumentService.Core.Interfaces;

/// <summary>
/// Repository interface for Document entity operations.
/// </summary>
public interface IDocumentRepository : IRepository<Document, long>
{
    /// <summary>
    /// Gets documents by tenant identifier.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="includeDeleted">Whether to include soft-deleted documents.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of documents belonging to the tenant.</returns>
    Task<IEnumerable<Document>> GetByTenantIdAsync(int tenantId, bool includeDeleted = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets documents by document type identifier.
    /// </summary>
    /// <param name="documentTypeId">The document type identifier.</param>
    /// <param name="includeDeleted">Whether to include soft-deleted documents.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of documents of the specified type.</returns>
    Task<IEnumerable<Document>> GetByDocumentTypeIdAsync(int documentTypeId, bool includeDeleted = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current version of a document by its parent document identifier.
    /// </summary>
    /// <param name="parentDocumentId">The parent document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current version of the document if found, otherwise null.</returns>
    Task<Document?> GetCurrentVersionAsync(long parentDocumentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all versions of a document.
    /// </summary>
    /// <param name="parentDocumentId">The parent document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of all document versions ordered by version number descending.</returns>
    Task<IEnumerable<Document>> GetVersionsAsync(long parentDocumentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets documents by content hash for deduplication.
    /// </summary>
    /// <param name="contentHash">The content hash.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of documents with the specified content hash.</returns>
    Task<IEnumerable<Document>> GetByContentHashAsync(string contentHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a document.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="deletedBy">The user who is deleting the document.</param>
    /// <param name="deletedReason">The reason for deletion.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SoftDeleteAsync(long documentId, string deletedBy, string? deletedReason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a soft-deleted document.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RestoreAsync(long documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Archives a document.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ArchiveAsync(long documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets archived documents for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of archived documents.</returns>
    Task<IEnumerable<Document>> GetArchivedDocumentsAsync(int tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets documents that need indexing (not indexed or updated since last index).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of documents requiring indexing.</returns>
    Task<IEnumerable<Document>> GetDocumentsNeedingIndexingAsync(CancellationToken cancellationToken = default);
}
