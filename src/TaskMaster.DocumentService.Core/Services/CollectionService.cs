using Microsoft.Extensions.Logging;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Interfaces;

namespace TaskMaster.DocumentService.Core.Services;

/// <summary>
/// Implementation of collection management service with business logic for CRUD operations and document relationships.
/// </summary>
public class CollectionService : ICollectionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CollectionService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionService"/> class.
    /// </summary>
    /// <param name="unitOfWork">The unit of work for database operations.</param>
    /// <param name="logger">The logger instance.</param>
    public CollectionService(
        IUnitOfWork unitOfWork,
        ILogger<CollectionService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<Collection> CreateCollectionAsync(
        int tenantId,
        string name,
        string? description,
        string slug,
        string? metadata,
        string? tags,
        string createdBy,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or empty.", nameof(name));

        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Slug cannot be null or empty.", nameof(slug));

        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ArgumentException("Created by cannot be null or empty.", nameof(createdBy));

        try
        {
            _logger.LogInformation(
                "Creating collection '{Name}' with slug '{Slug}' for tenant {TenantId} by user {CreatedBy}",
                name, slug, tenantId, createdBy);

            // Validate tenant exists
            var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
            if (tenant == null)
            {
                throw new InvalidOperationException($"Tenant with ID {tenantId} not found.");
            }

            // Check if slug already exists for this tenant
            var existingCollection = await _unitOfWork.Collections.GetBySlugAsync(slug, tenantId, cancellationToken);
            if (existingCollection != null)
            {
                throw new InvalidOperationException($"A collection with slug '{slug}' already exists for tenant {tenantId}.");
            }

            // Create collection entity
            var collection = new Collection
            {
                TenantId = tenantId,
                Name = name,
                Description = description,
                Slug = slug,
                Metadata = metadata,
                Tags = tags,
                IsPublished = false,
                SortOrder = 0,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                IsDeleted = false
            };

            await _unitOfWork.Collections.AddAsync(collection, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully created collection {CollectionId} with name '{Name}'",
                collection.Id, name);

            return collection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create collection '{Name}' for tenant {TenantId}", name, tenantId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Collection?> GetCollectionByIdAsync(long collectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving collection {CollectionId}", collectionId);

            var collection = await _unitOfWork.Collections.GetByIdAsync(collectionId, cancellationToken);

            if (collection == null)
            {
                _logger.LogWarning("Collection {CollectionId} not found", collectionId);
            }

            return collection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve collection {CollectionId}", collectionId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Collection?> GetCollectionBySlugAsync(string slug, int tenantId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Slug cannot be null or empty.", nameof(slug));

        try
        {
            _logger.LogDebug("Retrieving collection with slug '{Slug}' for tenant {TenantId}", slug, tenantId);

            return await _unitOfWork.Collections.GetBySlugAsync(slug, tenantId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve collection with slug '{Slug}' for tenant {TenantId}", slug, tenantId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Collection>> GetCollectionsByTenantAsync(
        int tenantId,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving collections for tenant {TenantId}, includeDeleted: {IncludeDeleted}",
                tenantId, includeDeleted);

            return await _unitOfWork.Collections.GetByTenantIdAsync(tenantId, includeDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve collections for tenant {TenantId}", tenantId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Collection>> GetPublishedCollectionsAsync(
        int tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving published collections for tenant {TenantId}", tenantId);

            return await _unitOfWork.Collections.GetPublishedCollectionsAsync(tenantId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve published collections for tenant {TenantId}", tenantId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Collection> UpdateCollectionAsync(
        long collectionId,
        string? name,
        string? description,
        string? slug,
        string? metadata,
        string? tags,
        string updatedBy,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(updatedBy))
            throw new ArgumentException("Updated by cannot be null or empty.", nameof(updatedBy));

        try
        {
            _logger.LogInformation("Updating collection {CollectionId} by user {UpdatedBy}",
                collectionId, updatedBy);

            var collection = await _unitOfWork.Collections.GetByIdAsync(collectionId, cancellationToken);
            if (collection == null)
            {
                throw new InvalidOperationException($"Collection with ID {collectionId} not found.");
            }

            if (collection.IsDeleted)
            {
                throw new InvalidOperationException($"Cannot update deleted collection {collectionId}.");
            }

            // Update properties if provided
            if (!string.IsNullOrWhiteSpace(name))
            {
                collection.Name = name;
            }

            if (description != null)
            {
                collection.Description = description;
            }

            if (!string.IsNullOrWhiteSpace(slug) && slug != collection.Slug)
            {
                // Check if new slug already exists
                var existingCollection = await _unitOfWork.Collections.GetBySlugAsync(slug, collection.TenantId, cancellationToken);
                if (existingCollection != null && existingCollection.Id != collectionId)
                {
                    throw new InvalidOperationException($"A collection with slug '{slug}' already exists for tenant {collection.TenantId}.");
                }
                collection.Slug = slug;
            }

            if (metadata != null)
            {
                collection.Metadata = metadata;
            }

            if (tags != null)
            {
                collection.Tags = tags;
            }

            collection.UpdatedAt = DateTime.UtcNow;
            collection.UpdatedBy = updatedBy;

            _unitOfWork.Collections.Update(collection);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully updated collection {CollectionId}", collectionId);

            return collection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update collection {CollectionId}", collectionId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Collection> PublishCollectionAsync(
        long collectionId,
        string publishedBy,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publishedBy))
            throw new ArgumentException("Published by cannot be null or empty.", nameof(publishedBy));

        try
        {
            _logger.LogInformation("Publishing collection {CollectionId} by user {PublishedBy}",
                collectionId, publishedBy);

            var collection = await _unitOfWork.Collections.GetByIdAsync(collectionId, cancellationToken);
            if (collection == null)
            {
                throw new InvalidOperationException($"Collection with ID {collectionId} not found.");
            }

            if (collection.IsDeleted)
            {
                throw new InvalidOperationException($"Cannot publish deleted collection {collectionId}.");
            }

            if (collection.IsPublished)
            {
                _logger.LogWarning("Collection {CollectionId} is already published", collectionId);
                return collection;
            }

            collection.IsPublished = true;
            collection.PublishedAt = DateTime.UtcNow;
            collection.PublishedBy = publishedBy;
            collection.UpdatedAt = DateTime.UtcNow;
            collection.UpdatedBy = publishedBy;

            _unitOfWork.Collections.Update(collection);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully published collection {CollectionId}", collectionId);

            return collection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish collection {CollectionId}", collectionId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Collection> UnpublishCollectionAsync(
        long collectionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Unpublishing collection {CollectionId}", collectionId);

            var collection = await _unitOfWork.Collections.GetByIdAsync(collectionId, cancellationToken);
            if (collection == null)
            {
                throw new InvalidOperationException($"Collection with ID {collectionId} not found.");
            }

            if (collection.IsDeleted)
            {
                throw new InvalidOperationException($"Cannot unpublish deleted collection {collectionId}.");
            }

            if (!collection.IsPublished)
            {
                _logger.LogWarning("Collection {CollectionId} is already unpublished", collectionId);
                return collection;
            }

            collection.IsPublished = false;
            collection.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Collections.Update(collection);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully unpublished collection {CollectionId}", collectionId);

            return collection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unpublish collection {CollectionId}", collectionId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Document>> GetDocumentsInCollectionAsync(
        long collectionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving documents for collection {CollectionId}", collectionId);

            // Verify collection exists
            var collection = await _unitOfWork.Collections.GetByIdAsync(collectionId, cancellationToken);
            if (collection == null)
            {
                throw new InvalidOperationException($"Collection with ID {collectionId} not found.");
            }

            return await _unitOfWork.Collections.GetDocumentsInCollectionAsync(collectionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve documents for collection {CollectionId}", collectionId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task AddDocumentToCollectionAsync(
        long collectionId,
        long documentId,
        int sortOrder,
        string addedBy,
        string? metadata = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(addedBy))
            throw new ArgumentException("Added by cannot be null or empty.", nameof(addedBy));

        try
        {
            _logger.LogInformation(
                "Adding document {DocumentId} to collection {CollectionId} by user {AddedBy}",
                documentId, collectionId, addedBy);

            // Verify collection exists
            var collection = await _unitOfWork.Collections.GetByIdAsync(collectionId, cancellationToken);
            if (collection == null)
            {
                throw new InvalidOperationException($"Collection with ID {collectionId} not found.");
            }

            if (collection.IsDeleted)
            {
                throw new InvalidOperationException($"Cannot add documents to deleted collection {collectionId}.");
            }

            // Verify document exists
            var document = await _unitOfWork.Documents.GetByIdAsync(documentId, cancellationToken);
            if (document == null)
            {
                throw new InvalidOperationException($"Document with ID {documentId} not found.");
            }

            if (document.IsDeleted)
            {
                throw new InvalidOperationException($"Cannot add deleted document {documentId} to collection.");
            }

            // Check if document is already in collection
            var exists = await _unitOfWork.Collections.IsDocumentInCollectionAsync(collectionId, documentId, cancellationToken);
            if (exists)
            {
                _logger.LogWarning(
                    "Document {DocumentId} is already in collection {CollectionId}",
                    documentId, collectionId);
                return;
            }

            // Create collection-document relationship
            var collectionDocument = new CollectionDocument
            {
                CollectionId = collectionId,
                DocumentId = documentId,
                SortOrder = sortOrder,
                AddedAt = DateTime.UtcNow,
                AddedBy = addedBy,
                Metadata = metadata
            };

            await _unitOfWork.Collections.AddDocumentToCollectionAsync(collectionDocument, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully added document {DocumentId} to collection {CollectionId}",
                documentId, collectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to add document {DocumentId} to collection {CollectionId}",
                documentId, collectionId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task RemoveDocumentFromCollectionAsync(
        long collectionId,
        long documentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Removing document {DocumentId} from collection {CollectionId}",
                documentId, collectionId);

            // Verify collection exists
            var collection = await _unitOfWork.Collections.GetByIdAsync(collectionId, cancellationToken);
            if (collection == null)
            {
                throw new InvalidOperationException($"Collection with ID {collectionId} not found.");
            }

            await _unitOfWork.Collections.RemoveDocumentFromCollectionAsync(collectionId, documentId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully removed document {DocumentId} from collection {CollectionId}",
                documentId, collectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to remove document {DocumentId} from collection {CollectionId}",
                documentId, collectionId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task AddDocumentsToCollectionAsync(
        long collectionId,
        IEnumerable<long> documentIds,
        string addedBy,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(addedBy))
            throw new ArgumentException("Added by cannot be null or empty.", nameof(addedBy));

        if (documentIds == null || !documentIds.Any())
            throw new ArgumentException("Document IDs cannot be null or empty.", nameof(documentIds));

        try
        {
            _logger.LogInformation(
                "Adding {Count} documents to collection {CollectionId} by user {AddedBy}",
                documentIds.Count(), collectionId, addedBy);

            // Verify collection exists
            var collection = await _unitOfWork.Collections.GetByIdAsync(collectionId, cancellationToken);
            if (collection == null)
            {
                throw new InvalidOperationException($"Collection with ID {collectionId} not found.");
            }

            if (collection.IsDeleted)
            {
                throw new InvalidOperationException($"Cannot add documents to deleted collection {collectionId}.");
            }

            // Get current max sort order
            var existingDocuments = await _unitOfWork.Collections.GetDocumentsInCollectionAsync(collectionId, cancellationToken);
            var maxSortOrder = existingDocuments.Any() ? existingDocuments.Max(d => d.Id) : 0;
            var sortOrder = (int)maxSortOrder;

            foreach (var documentId in documentIds)
            {
                // Verify document exists
                var document = await _unitOfWork.Documents.GetByIdAsync(documentId, cancellationToken);
                if (document == null)
                {
                    _logger.LogWarning("Skipping document {DocumentId} - not found", documentId);
                    continue;
                }

                if (document.IsDeleted)
                {
                    _logger.LogWarning("Skipping document {DocumentId} - deleted", documentId);
                    continue;
                }

                // Check if already in collection
                var exists = await _unitOfWork.Collections.IsDocumentInCollectionAsync(collectionId, documentId, cancellationToken);
                if (exists)
                {
                    _logger.LogWarning("Document {DocumentId} already in collection {CollectionId}", documentId, collectionId);
                    continue;
                }

                sortOrder++;

                var collectionDocument = new CollectionDocument
                {
                    CollectionId = collectionId,
                    DocumentId = documentId,
                    SortOrder = sortOrder,
                    AddedAt = DateTime.UtcNow,
                    AddedBy = addedBy
                };

                await _unitOfWork.Collections.AddDocumentToCollectionAsync(collectionDocument, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully added documents to collection {CollectionId}",
                collectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add documents to collection {CollectionId}", collectionId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task ReorderDocumentsInCollectionAsync(
        long collectionId,
        Dictionary<long, int> documentSortOrders,
        CancellationToken cancellationToken = default)
    {
        if (documentSortOrders == null || !documentSortOrders.Any())
            throw new ArgumentException("Document sort orders cannot be null or empty.", nameof(documentSortOrders));

        try
        {
            _logger.LogInformation(
                "Reordering documents in collection {CollectionId}",
                collectionId);

            // Verify collection exists
            var collection = await _unitOfWork.Collections.GetByIdAsync(collectionId, cancellationToken);
            if (collection == null)
            {
                throw new InvalidOperationException($"Collection with ID {collectionId} not found.");
            }

            if (collection.IsDeleted)
            {
                throw new InvalidOperationException($"Cannot reorder documents in deleted collection {collectionId}.");
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            foreach (var kvp in documentSortOrders)
            {
                var documentId = kvp.Key;
                var sortOrder = kvp.Value;

                // Verify document is in collection
                var exists = await _unitOfWork.Collections.IsDocumentInCollectionAsync(collectionId, documentId, cancellationToken);
                if (!exists)
                {
                    _logger.LogWarning(
                        "Document {DocumentId} not in collection {CollectionId}, skipping reorder",
                        documentId, collectionId);
                    continue;
                }

                // Update would require accessing the DbContext directly or creating a specific repository method
                // For now, we'll log that this needs to be implemented at the repository level
                _logger.LogWarning("Reordering requires repository-level support for updating CollectionDocument entities");
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully reordered documents in collection {CollectionId}",
                collectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reorder documents in collection {CollectionId}", collectionId);
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task DeleteCollectionAsync(
        long collectionId,
        string deletedBy,
        string? deletedReason = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deletedBy))
            throw new ArgumentException("Deleted by cannot be null or empty.", nameof(deletedBy));

        try
        {
            _logger.LogInformation(
                "Soft deleting collection {CollectionId} by user {DeletedBy}",
                collectionId, deletedBy);

            await _unitOfWork.Collections.SoftDeleteAsync(collectionId, deletedBy, deletedReason, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully soft deleted collection {CollectionId}", collectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to soft delete collection {CollectionId}", collectionId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task RestoreCollectionAsync(long collectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Restoring collection {CollectionId}", collectionId);

            await _unitOfWork.Collections.RestoreAsync(collectionId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully restored collection {CollectionId}", collectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore collection {CollectionId}", collectionId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task PermanentlyDeleteCollectionAsync(long collectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogWarning("Permanently deleting collection {CollectionId}", collectionId);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var collection = await _unitOfWork.Collections.GetByIdAsync(collectionId, cancellationToken);
            if (collection == null)
            {
                throw new InvalidOperationException($"Collection with ID {collectionId} not found.");
            }

            // Remove all document relationships first (cascade delete will handle this)
            _unitOfWork.Collections.Remove(collection);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogWarning("Successfully permanently deleted collection {CollectionId}", collectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to permanently delete collection {CollectionId}", collectionId);
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
