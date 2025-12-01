using TaskMaster.DocumentService.Core.Entities;

namespace TaskMaster.DocumentService.Core.Interfaces;

/// <summary>
/// Service interface for collection management operations with business logic.
/// </summary>
public interface ICollectionService
{
    /// <summary>
    /// Creates a new collection.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="name">The collection name.</param>
    /// <param name="description">The collection description.</param>
    /// <param name="slug">The URL-friendly slug for the collection.</param>
    /// <param name="metadata">Optional metadata as JSON string.</param>
    /// <param name="tags">Optional tags as JSON string.</param>
    /// <param name="createdBy">The user creating the collection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created collection.</returns>
    Task<Collection> CreateCollectionAsync(
        int tenantId,
        string name,
        string? description,
        string slug,
        string? metadata,
        string? tags,
        string createdBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a collection by its identifier.
    /// </summary>
    /// <param name="collectionId">The collection identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The collection if found, otherwise null.</returns>
    Task<Collection?> GetCollectionByIdAsync(long collectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a collection by its slug.
    /// </summary>
    /// <param name="slug">The collection slug.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The collection if found, otherwise null.</returns>
    Task<Collection?> GetCollectionBySlugAsync(string slug, int tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all collections for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="includeDeleted">Whether to include soft-deleted collections.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of collections.</returns>
    Task<IEnumerable<Collection>> GetCollectionsByTenantAsync(
        int tenantId,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all published collections for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of published collections.</returns>
    Task<IEnumerable<Collection>> GetPublishedCollectionsAsync(
        int tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates collection metadata and properties.
    /// </summary>
    /// <param name="collectionId">The collection identifier.</param>
    /// <param name="name">The updated name.</param>
    /// <param name="description">The updated description.</param>
    /// <param name="slug">The updated slug.</param>
    /// <param name="metadata">The updated metadata as JSON string.</param>
    /// <param name="tags">The updated tags as JSON string.</param>
    /// <param name="updatedBy">The user updating the collection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated collection.</returns>
    Task<Collection> UpdateCollectionAsync(
        long collectionId,
        string? name,
        string? description,
        string? slug,
        string? metadata,
        string? tags,
        string updatedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a collection.
    /// </summary>
    /// <param name="collectionId">The collection identifier.</param>
    /// <param name="publishedBy">The user publishing the collection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The published collection.</returns>
    Task<Collection> PublishCollectionAsync(
        long collectionId,
        string publishedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unpublishes a collection.
    /// </summary>
    /// <param name="collectionId">The collection identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The unpublished collection.</returns>
    Task<Collection> UnpublishCollectionAsync(
        long collectionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all documents in a collection.
    /// </summary>
    /// <param name="collectionId">The collection identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of documents.</returns>
    Task<IEnumerable<Document>> GetDocumentsInCollectionAsync(
        long collectionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a document to a collection.
    /// </summary>
    /// <param name="collectionId">The collection identifier.</param>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="sortOrder">The sort order for the document within the collection.</param>
    /// <param name="addedBy">The user adding the document.</param>
    /// <param name="metadata">Optional metadata for the relationship as JSON string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddDocumentToCollectionAsync(
        long collectionId,
        long documentId,
        int sortOrder,
        string addedBy,
        string? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a document from a collection.
    /// </summary>
    /// <param name="collectionId">The collection identifier.</param>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveDocumentFromCollectionAsync(
        long collectionId,
        long documentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple documents to a collection.
    /// </summary>
    /// <param name="collectionId">The collection identifier.</param>
    /// <param name="documentIds">The document identifiers to add.</param>
    /// <param name="addedBy">The user adding the documents.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddDocumentsToCollectionAsync(
        long collectionId,
        IEnumerable<long> documentIds,
        string addedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reorders documents in a collection.
    /// </summary>
    /// <param name="collectionId">The collection identifier.</param>
    /// <param name="documentSortOrders">Dictionary mapping document IDs to their new sort orders.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ReorderDocumentsInCollectionAsync(
        long collectionId,
        Dictionary<long, int> documentSortOrders,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a collection.
    /// </summary>
    /// <param name="collectionId">The collection identifier.</param>
    /// <param name="deletedBy">The user deleting the collection.</param>
    /// <param name="deletedReason">The reason for deletion.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteCollectionAsync(
        long collectionId,
        string deletedBy,
        string? deletedReason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a soft-deleted collection.
    /// </summary>
    /// <param name="collectionId">The collection identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RestoreCollectionAsync(long collectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently deletes a collection and all its relationships.
    /// </summary>
    /// <param name="collectionId">The collection identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PermanentlyDeleteCollectionAsync(long collectionId, CancellationToken cancellationToken = default);
}
