using TaskMaster.DocumentService.Core.Entities;

namespace TaskMaster.DocumentService.Core.Interfaces;

/// <summary>
/// Repository interface for collection data access operations.
/// </summary>
public interface ICollectionRepository
{
    /// <summary>
    /// Gets a collection by its ID.
    /// </summary>
    /// <param name="id">The collection ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The collection if found, otherwise null.</returns>
    Task<Collection?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a collection by tenant ID and slug.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="slug">The collection slug.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The collection if found, otherwise null.</returns>
    Task<Collection?> GetBySlugAsync(int tenantId, string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all collections for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="includeDeleted">Whether to include soft-deleted collections.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of collections.</returns>
    Task<List<Collection>> GetByTenantIdAsync(int tenantId, bool includeDeleted = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets published collections for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of published collections.</returns>
    Task<List<Collection>> GetPublishedByTenantIdAsync(int tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new collection.
    /// </summary>
    /// <param name="collection">The collection to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created collection.</returns>
    Task<Collection> CreateAsync(Collection collection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing collection.
    /// </summary>
    /// <param name="collection">The collection to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated collection.</returns>
    Task<Collection> UpdateAsync(Collection collection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a collection (soft delete).
    /// </summary>
    /// <param name="id">The collection ID.</param>
    /// <param name="deletedBy">The user performing the deletion.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted successfully, otherwise false.</returns>
    Task<bool> DeleteAsync(long id, string deletedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a collection.
    /// </summary>
    /// <param name="id">The collection ID.</param>
    /// <param name="publishedBy">The user publishing the collection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if published successfully, otherwise false.</returns>
    Task<bool> PublishAsync(long id, string publishedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unpublishes a collection.
    /// </summary>
    /// <param name="id">The collection ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if unpublished successfully, otherwise false.</returns>
    Task<bool> UnpublishAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a document to a collection.
    /// </summary>
    /// <param name="collectionDocument">The collection-document relationship to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created collection-document relationship.</returns>
    Task<CollectionDocument> AddDocumentAsync(CollectionDocument collectionDocument, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a document from a collection.
    /// </summary>
    /// <param name="collectionId">The collection ID.</param>
    /// <param name="documentId">The document ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if removed successfully, otherwise false.</returns>
    Task<bool> RemoveDocumentAsync(long collectionId, long documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all documents in a collection.
    /// </summary>
    /// <param name="collectionId">The collection ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of collection-document relationships.</returns>
    Task<List<CollectionDocument>> GetDocumentsAsync(long collectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a document exists in a collection.
    /// </summary>
    /// <param name="collectionId">The collection ID.</param>
    /// <param name="documentId">The document ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the document exists in the collection, otherwise false.</returns>
    Task<bool> DocumentExistsInCollectionAsync(long collectionId, long documentId, CancellationToken cancellationToken = default);
}
