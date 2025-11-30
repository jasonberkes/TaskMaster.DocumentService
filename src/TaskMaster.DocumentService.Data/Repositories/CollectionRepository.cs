using Microsoft.EntityFrameworkCore;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Interfaces;

namespace TaskMaster.DocumentService.Data.Repositories;

/// <summary>
/// Repository implementation for Collection entity operations.
/// </summary>
public class CollectionRepository : Repository<Collection, long>, ICollectionRepository
{
    private readonly DbSet<CollectionDocument> _collectionDocuments;

    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public CollectionRepository(DocumentServiceDbContext context) : base(context)
    {
        _collectionDocuments = context.Set<CollectionDocument>();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Collection>> GetByTenantIdAsync(int tenantId, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(c => c.TenantId == tenantId);

        if (!includeDeleted)
        {
            query = query.Where(c => !c.IsDeleted);
        }

        return await query
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Collection?> GetBySlugAsync(string slug, int tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(c => c.Slug == slug && c.TenantId == tenantId && !c.IsDeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Collection>> GetPublishedCollectionsAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.TenantId == tenantId && c.IsPublished && !c.IsDeleted)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.PublishedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Document>> GetDocumentsInCollectionAsync(long collectionId, CancellationToken cancellationToken = default)
    {
        return await _collectionDocuments
            .Where(cd => cd.CollectionId == collectionId)
            .Include(cd => cd.Document)
            .OrderBy(cd => cd.SortOrder)
            .Select(cd => cd.Document)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task AddDocumentToCollectionAsync(CollectionDocument collectionDocument, CancellationToken cancellationToken = default)
    {
        await _collectionDocuments.AddAsync(collectionDocument, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task RemoveDocumentFromCollectionAsync(long collectionId, long documentId, CancellationToken cancellationToken = default)
    {
        var collectionDocument = await _collectionDocuments
            .FirstOrDefaultAsync(cd => cd.CollectionId == collectionId && cd.DocumentId == documentId, cancellationToken);

        if (collectionDocument != null)
        {
            _collectionDocuments.Remove(collectionDocument);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsDocumentInCollectionAsync(long collectionId, long documentId, CancellationToken cancellationToken = default)
    {
        return await _collectionDocuments
            .AnyAsync(cd => cd.CollectionId == collectionId && cd.DocumentId == documentId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task SoftDeleteAsync(long collectionId, string deletedBy, string? deletedReason = null, CancellationToken cancellationToken = default)
    {
        var collection = await GetByIdAsync(collectionId, cancellationToken);
        if (collection == null)
        {
            throw new InvalidOperationException($"Collection with ID {collectionId} not found.");
        }

        collection.IsDeleted = true;
        collection.DeletedAt = DateTime.UtcNow;
        collection.DeletedBy = deletedBy;
        collection.DeletedReason = deletedReason;

        Update(collection);
    }

    /// <inheritdoc/>
    public async Task RestoreAsync(long collectionId, CancellationToken cancellationToken = default)
    {
        var collection = await _dbSet
            .FirstOrDefaultAsync(c => c.Id == collectionId, cancellationToken);

        if (collection == null)
        {
            throw new InvalidOperationException($"Collection with ID {collectionId} not found.");
        }

        collection.IsDeleted = false;
        collection.DeletedAt = null;
        collection.DeletedBy = null;
        collection.DeletedReason = null;

        Update(collection);
    }
}
