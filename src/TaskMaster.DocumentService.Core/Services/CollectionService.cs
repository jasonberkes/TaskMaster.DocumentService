using Microsoft.Extensions.Logging;
using TaskMaster.DocumentService.Core.DTOs;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Interfaces;

namespace TaskMaster.DocumentService.Core.Services;

/// <summary>
/// Service implementation for collection business logic operations.
/// </summary>
public class CollectionService : ICollectionService
{
    private readonly ICollectionRepository _repository;
    private readonly ILogger<CollectionService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionService"/> class.
    /// </summary>
    /// <param name="repository">The collection repository.</param>
    /// <param name="logger">The logger instance.</param>
    public CollectionService(ICollectionRepository repository, ILogger<CollectionService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<CollectionDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting collection by ID: {CollectionId}", id);
            var collection = await _repository.GetByIdAsync(id, cancellationToken);
            return collection != null ? MapToDto(collection) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collection by ID: {CollectionId}", id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<CollectionDto?> GetBySlugAsync(int tenantId, string slug, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting collection by slug: {Slug} for tenant: {TenantId}", slug, tenantId);
            var collection = await _repository.GetBySlugAsync(tenantId, slug, cancellationToken);
            return collection != null ? MapToDto(collection) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collection by slug: {Slug} for tenant: {TenantId}", slug, tenantId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<CollectionDto>> GetByTenantIdAsync(int tenantId, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting collections for tenant: {TenantId}, includeDeleted: {IncludeDeleted}", tenantId, includeDeleted);
            var collections = await _repository.GetByTenantIdAsync(tenantId, includeDeleted, cancellationToken);
            return collections.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collections for tenant: {TenantId}", tenantId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<CollectionDto>> GetPublishedByTenantIdAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting published collections for tenant: {TenantId}", tenantId);
            var collections = await _repository.GetPublishedByTenantIdAsync(tenantId, cancellationToken);
            return collections.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting published collections for tenant: {TenantId}", tenantId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<CollectionDto> CreateAsync(CreateCollectionDto createDto, string createdBy, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating collection: {Name} for tenant: {TenantId}", createDto.Name, createDto.TenantId);

            // Check if slug already exists for this tenant
            var existingCollection = await _repository.GetBySlugAsync(createDto.TenantId, createDto.Slug, cancellationToken);
            if (existingCollection != null)
            {
                _logger.LogWarning("Collection with slug {Slug} already exists for tenant {TenantId}", createDto.Slug, createDto.TenantId);
                throw new InvalidOperationException($"A collection with slug '{createDto.Slug}' already exists for this tenant.");
            }

            var collection = new Collection
            {
                TenantId = createDto.TenantId,
                Name = createDto.Name,
                Description = createDto.Description,
                Slug = createDto.Slug,
                CoverImageUrl = createDto.CoverImageUrl,
                Metadata = createDto.Metadata,
                Tags = createDto.Tags,
                SortOrder = createDto.SortOrder,
                CreatedBy = createdBy,
                Status = "Draft",
                IsPublished = false
            };

            var created = await _repository.CreateAsync(collection, cancellationToken);
            _logger.LogInformation("Collection created successfully with ID: {CollectionId}", created.Id);

            return MapToDto(created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating collection: {Name}", createDto.Name);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<CollectionDto?> UpdateAsync(long id, UpdateCollectionDto updateDto, string updatedBy, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating collection: {CollectionId}", id);

            var collection = await _repository.GetByIdAsync(id, cancellationToken);
            if (collection == null)
            {
                _logger.LogWarning("Collection not found: {CollectionId}", id);
                return null;
            }

            // Check if slug is being changed and if it already exists for this tenant
            if (collection.Slug != updateDto.Slug)
            {
                var existingCollection = await _repository.GetBySlugAsync(collection.TenantId, updateDto.Slug, cancellationToken);
                if (existingCollection != null && existingCollection.Id != id)
                {
                    _logger.LogWarning("Collection with slug {Slug} already exists for tenant {TenantId}", updateDto.Slug, collection.TenantId);
                    throw new InvalidOperationException($"A collection with slug '{updateDto.Slug}' already exists for this tenant.");
                }
            }

            collection.Name = updateDto.Name;
            collection.Description = updateDto.Description;
            collection.Slug = updateDto.Slug;
            collection.Status = updateDto.Status;
            collection.CoverImageUrl = updateDto.CoverImageUrl;
            collection.Metadata = updateDto.Metadata;
            collection.Tags = updateDto.Tags;
            collection.SortOrder = updateDto.SortOrder;
            collection.UpdatedBy = updatedBy;

            var updated = await _repository.UpdateAsync(collection, cancellationToken);
            _logger.LogInformation("Collection updated successfully: {CollectionId}", id);

            return MapToDto(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating collection: {CollectionId}", id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(long id, string deletedBy, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting collection: {CollectionId}", id);
            var result = await _repository.DeleteAsync(id, deletedBy, cancellationToken);

            if (result)
            {
                _logger.LogInformation("Collection deleted successfully: {CollectionId}", id);
            }
            else
            {
                _logger.LogWarning("Collection not found for deletion: {CollectionId}", id);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting collection: {CollectionId}", id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> PublishAsync(long id, string publishedBy, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Publishing collection: {CollectionId}", id);
            var result = await _repository.PublishAsync(id, publishedBy, cancellationToken);

            if (result)
            {
                _logger.LogInformation("Collection published successfully: {CollectionId}", id);
            }
            else
            {
                _logger.LogWarning("Collection not found for publishing: {CollectionId}", id);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing collection: {CollectionId}", id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> UnpublishAsync(long id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Unpublishing collection: {CollectionId}", id);
            var result = await _repository.UnpublishAsync(id, cancellationToken);

            if (result)
            {
                _logger.LogInformation("Collection unpublished successfully: {CollectionId}", id);
            }
            else
            {
                _logger.LogWarning("Collection not found for unpublishing: {CollectionId}", id);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unpublishing collection: {CollectionId}", id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<CollectionDocumentDto?> AddDocumentAsync(long collectionId, AddDocumentToCollectionDto addDocumentDto, string addedBy, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Adding document {DocumentId} to collection {CollectionId}", addDocumentDto.DocumentId, collectionId);

            // Check if collection exists
            var collection = await _repository.GetByIdAsync(collectionId, cancellationToken);
            if (collection == null)
            {
                _logger.LogWarning("Collection not found: {CollectionId}", collectionId);
                return null;
            }

            // Check if document already exists in collection
            var exists = await _repository.DocumentExistsInCollectionAsync(collectionId, addDocumentDto.DocumentId, cancellationToken);
            if (exists)
            {
                _logger.LogWarning("Document {DocumentId} already exists in collection {CollectionId}", addDocumentDto.DocumentId, collectionId);
                throw new InvalidOperationException($"Document {addDocumentDto.DocumentId} is already in this collection.");
            }

            var collectionDocument = new CollectionDocument
            {
                CollectionId = collectionId,
                DocumentId = addDocumentDto.DocumentId,
                SortOrder = addDocumentDto.SortOrder,
                Notes = addDocumentDto.Notes,
                AddedBy = addedBy
            };

            var created = await _repository.AddDocumentAsync(collectionDocument, cancellationToken);
            _logger.LogInformation("Document added to collection successfully: {DocumentId} -> {CollectionId}", addDocumentDto.DocumentId, collectionId);

            return MapToCollectionDocumentDto(created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding document {DocumentId} to collection {CollectionId}", addDocumentDto.DocumentId, collectionId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> RemoveDocumentAsync(long collectionId, long documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Removing document {DocumentId} from collection {CollectionId}", documentId, collectionId);
            var result = await _repository.RemoveDocumentAsync(collectionId, documentId, cancellationToken);

            if (result)
            {
                _logger.LogInformation("Document removed from collection successfully: {DocumentId} -> {CollectionId}", documentId, collectionId);
            }
            else
            {
                _logger.LogWarning("Document or collection not found for removal: {DocumentId} -> {CollectionId}", documentId, collectionId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing document {DocumentId} from collection {CollectionId}", documentId, collectionId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<CollectionDocumentDto>> GetDocumentsAsync(long collectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting documents for collection: {CollectionId}", collectionId);
            var documents = await _repository.GetDocumentsAsync(collectionId, cancellationToken);
            return documents.Select(MapToCollectionDocumentDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting documents for collection: {CollectionId}", collectionId);
            throw;
        }
    }

    private static CollectionDto MapToDto(Collection collection)
    {
        return new CollectionDto
        {
            Id = collection.Id,
            TenantId = collection.TenantId,
            Name = collection.Name,
            Description = collection.Description,
            Slug = collection.Slug,
            Status = collection.Status,
            IsPublished = collection.IsPublished,
            PublishedAt = collection.PublishedAt,
            PublishedBy = collection.PublishedBy,
            CoverImageUrl = collection.CoverImageUrl,
            Metadata = collection.Metadata,
            Tags = collection.Tags,
            SortOrder = collection.SortOrder,
            CreatedAt = collection.CreatedAt,
            CreatedBy = collection.CreatedBy,
            UpdatedAt = collection.UpdatedAt,
            UpdatedBy = collection.UpdatedBy,
            DocumentCount = collection.CollectionDocuments?.Count ?? 0
        };
    }

    private static CollectionDocumentDto MapToCollectionDocumentDto(CollectionDocument collectionDocument)
    {
        return new CollectionDocumentDto
        {
            Id = collectionDocument.Id,
            CollectionId = collectionDocument.CollectionId,
            DocumentId = collectionDocument.DocumentId,
            SortOrder = collectionDocument.SortOrder,
            AddedAt = collectionDocument.AddedAt,
            AddedBy = collectionDocument.AddedBy,
            Notes = collectionDocument.Notes
        };
    }
}
