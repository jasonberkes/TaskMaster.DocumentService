using TaskMaster.DocumentService.Core.Entities;

namespace TaskMaster.DocumentService.Core.Interfaces;

/// <summary>
/// Repository interface for document data access operations
/// </summary>
public interface IDocumentRepository
{
    /// <summary>
    /// Gets a document by its identifier
    /// </summary>
    /// <param name="id">The document identifier</param>
    /// <param name="includeDeleted">Whether to include soft-deleted documents</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The document if found, null otherwise</returns>
    Task<Document?> GetByIdAsync(long id, bool includeDeleted = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets documents by tenant identifier with pagination
    /// </summary>
    /// <param name="tenantId">The tenant identifier</param>
    /// <param name="skip">Number of records to skip</param>
    /// <param name="take">Number of records to take</param>
    /// <param name="includeDeleted">Whether to include soft-deleted documents</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of documents</returns>
    Task<IEnumerable<Document>> GetByTenantIdAsync(int tenantId, int skip = 0, int take = 50, bool includeDeleted = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all versions of a document
    /// </summary>
    /// <param name="parentDocumentId">The parent document identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of document versions ordered by version number</returns>
    Task<IEnumerable<Document>> GetVersionsAsync(long parentDocumentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current version of a document by parent document ID
    /// </summary>
    /// <param name="parentDocumentId">The parent document identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The current version of the document</returns>
    Task<Document?> GetCurrentVersionAsync(long parentDocumentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a document with the given content hash exists for a tenant
    /// </summary>
    /// <param name="tenantId">The tenant identifier</param>
    /// <param name="contentHash">The content hash</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if a duplicate exists, false otherwise</returns>
    Task<bool> ExistsByContentHashAsync(int tenantId, string contentHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new document
    /// </summary>
    /// <param name="document">The document to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created document with ID assigned</returns>
    Task<Document> CreateAsync(Document document, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing document
    /// </summary>
    /// <param name="document">The document to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated document</returns>
    Task<Document> UpdateAsync(Document document, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes a document
    /// </summary>
    /// <param name="id">The document identifier</param>
    /// <param name="deletedBy">The user performing the deletion</param>
    /// <param name="deletedReason">The reason for deletion</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully, false if not found</returns>
    Task<bool> SoftDeleteAsync(long id, string? deletedBy, string? deletedReason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Archives a document
    /// </summary>
    /// <param name="id">The document identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if archived successfully, false if not found</returns>
    Task<bool> ArchiveAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unarchives a document
    /// </summary>
    /// <param name="id">The document identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if unarchived successfully, false if not found</returns>
    Task<bool> UnarchiveAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of documents for a tenant
    /// </summary>
    /// <param name="tenantId">The tenant identifier</param>
    /// <param name="includeDeleted">Whether to include soft-deleted documents</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The count of documents</returns>
    Task<int> GetCountByTenantIdAsync(int tenantId, bool includeDeleted = false, CancellationToken cancellationToken = default);
}
