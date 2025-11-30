using TaskMaster.DocumentService.Core.Entities;

namespace TaskMaster.DocumentService.Core.Interfaces;

/// <summary>
/// Repository interface for Collection entity operations.
/// </summary>
public interface ICollectionRepository : IRepository<Collection, long>
{
    /// <summary>
    /// Gets all collections for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="includeDeleted">Whether to include soft-deleted collections.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of collections for the specified tenant.</returns>
    Task<IEnumerable<Collection>> GetByTenantIdAsync(int tenantId, bool includeDeleted = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a collection by its slug.
    /// </summary>
    /// <param name="slug">The collection slug.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The collection if found, otherwise null.</returns>
    Task<Collection?> GetBySlugAsync(string slug, int tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all published collections for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of published collections.</returns>
    Task<IEnumerable<Collection>> GetPublishedCollectionsAsync(int tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all documents in a collection.
    /// </summary>
    /// <param name="collectionId">The collection identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of documents in the specified collection.</returns>
    Task<IEnumerable<Document>> GetDocumentsInCollectionAsync(long collectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a document to a collection.
    /// </summary>
    /// <param name="collectionDocument">The collection-document relationship.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddDocumentToCollectionAsync(CollectionDocument collectionDocument, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a document from a collection.
    /// </summary>
    /// <param name="collectionId">The collection identifier.</param>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveDocumentFromCollectionAsync(long collectionId, long documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a document exists in a collection.
    /// </summary>
    /// <param name="collectionId">The collection identifier.</param>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the document exists in the collection, otherwise false.</returns>
    Task<bool> IsDocumentInCollectionAsync(long collectionId, long documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a collection.
    /// </summary>
    /// <param name="collectionId">The collection identifier.</param>
    /// <param name="deletedBy">The user deleting the collection.</param>
    /// <param name="deletedReason">The reason for deletion.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SoftDeleteAsync(long collectionId, string deletedBy, string? deletedReason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a soft-deleted collection.
    /// </summary>
    /// <param name="collectionId">The collection identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RestoreAsync(long collectionId, CancellationToken cancellationToken = default);
}
