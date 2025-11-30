using TaskMaster.DocumentService.Core.DTOs;

namespace TaskMaster.DocumentService.Core.Interfaces;

/// <summary>
/// Service interface for collection business logic operations.
/// </summary>
public interface ICollectionService
{
    /// <summary>
    /// Gets a collection by its ID.
    /// </summary>
    /// <param name="id">The collection ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The collection DTO if found, otherwise null.</returns>
    Task<CollectionDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a collection by tenant ID and slug.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="slug">The collection slug.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The collection DTO if found, otherwise null.</returns>
    Task<CollectionDto?> GetBySlugAsync(int tenantId, string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all collections for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="includeDeleted">Whether to include soft-deleted collections.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of collection DTOs.</returns>
    Task<List<CollectionDto>> GetByTenantIdAsync(int tenantId, bool includeDeleted = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets published collections for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of published collection DTOs.</returns>
    Task<List<CollectionDto>> GetPublishedByTenantIdAsync(int tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new collection.
    /// </summary>
    /// <param name="createDto">The collection creation data.</param>
    /// <param name="createdBy">The user creating the collection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created collection DTO.</returns>
    Task<CollectionDto> CreateAsync(CreateCollectionDto createDto, string createdBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing collection.
    /// </summary>
    /// <param name="id">The collection ID.</param>
    /// <param name="updateDto">The collection update data.</param>
    /// <param name="updatedBy">The user updating the collection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated collection DTO if successful, otherwise null.</returns>
    Task<CollectionDto?> UpdateAsync(long id, UpdateCollectionDto updateDto, string updatedBy, CancellationToken cancellationToken = default);

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
    /// <param name="collectionId">The collection ID.</param>
    /// <param name="addDocumentDto">The document addition data.</param>
    /// <param name="addedBy">The user adding the document.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created collection-document DTO if successful, otherwise null.</returns>
    Task<CollectionDocumentDto?> AddDocumentAsync(long collectionId, AddDocumentToCollectionDto addDocumentDto, string addedBy, CancellationToken cancellationToken = default);

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
    /// <returns>A list of collection-document DTOs.</returns>
    Task<List<CollectionDocumentDto>> GetDocumentsAsync(long collectionId, CancellationToken cancellationToken = default);
}
