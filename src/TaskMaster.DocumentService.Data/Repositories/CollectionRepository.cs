using Microsoft.EntityFrameworkCore;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Interfaces;
using TaskMaster.DocumentService.Data.Data;

namespace TaskMaster.DocumentService.Data.Repositories;

/// <summary>
/// Repository implementation for collection data access operations.
/// </summary>
public class CollectionRepository : ICollectionRepository
{
    private readonly DocumentServiceDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public CollectionRepository(DocumentServiceDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc/>
    public async Task<Collection?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.Collections
            .Include(c => c.CollectionDocuments)
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Collection?> GetBySlugAsync(int tenantId, string slug, CancellationToken cancellationToken = default)
    {
        return await _context.Collections
            .Include(c => c.CollectionDocuments)
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Slug == slug && !c.IsDeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<Collection>> GetByTenantIdAsync(int tenantId, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Collections
            .Include(c => c.CollectionDocuments)
            .Where(c => c.TenantId == tenantId);

        if (!includeDeleted)
        {
            query = query.Where(c => !c.IsDeleted);
        }

        return await query
            .OrderBy(c => c.SortOrder)
            .ThenByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<Collection>> GetPublishedByTenantIdAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Collections
            .Include(c => c.CollectionDocuments)
            .Where(c => c.TenantId == tenantId && c.IsPublished && !c.IsDeleted)
            .OrderBy(c => c.SortOrder)
            .ThenByDescending(c => c.PublishedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Collection> CreateAsync(Collection collection, CancellationToken cancellationToken = default)
    {
        collection.CreatedAt = DateTime.UtcNow;
        _context.Collections.Add(collection);
        await _context.SaveChangesAsync(cancellationToken);
        return collection;
    }

    /// <inheritdoc/>
    public async Task<Collection> UpdateAsync(Collection collection, CancellationToken cancellationToken = default)
    {
        collection.UpdatedAt = DateTime.UtcNow;
        _context.Collections.Update(collection);
        await _context.SaveChangesAsync(cancellationToken);
        return collection;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(long id, string deletedBy, CancellationToken cancellationToken = default)
    {
        var collection = await _context.Collections
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);

        if (collection == null)
        {
            return false;
        }

        collection.IsDeleted = true;
        collection.DeletedAt = DateTime.UtcNow;
        collection.DeletedBy = deletedBy;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> PublishAsync(long id, string publishedBy, CancellationToken cancellationToken = default)
    {
        var collection = await _context.Collections
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);

        if (collection == null)
        {
            return false;
        }

        collection.IsPublished = true;
        collection.PublishedAt = DateTime.UtcNow;
        collection.PublishedBy = publishedBy;
        collection.Status = "Published";
        collection.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> UnpublishAsync(long id, CancellationToken cancellationToken = default)
    {
        var collection = await _context.Collections
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);

        if (collection == null)
        {
            return false;
        }

        collection.IsPublished = false;
        collection.Status = "Draft";
        collection.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc/>
    public async Task<CollectionDocument> AddDocumentAsync(CollectionDocument collectionDocument, CancellationToken cancellationToken = default)
    {
        collectionDocument.AddedAt = DateTime.UtcNow;
        _context.CollectionDocuments.Add(collectionDocument);
        await _context.SaveChangesAsync(cancellationToken);
        return collectionDocument;
    }

    /// <inheritdoc/>
    public async Task<bool> RemoveDocumentAsync(long collectionId, long documentId, CancellationToken cancellationToken = default)
    {
        var collectionDocument = await _context.CollectionDocuments
            .FirstOrDefaultAsync(cd => cd.CollectionId == collectionId && cd.DocumentId == documentId, cancellationToken);

        if (collectionDocument == null)
        {
            return false;
        }

        _context.CollectionDocuments.Remove(collectionDocument);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc/>
    public async Task<List<CollectionDocument>> GetDocumentsAsync(long collectionId, CancellationToken cancellationToken = default)
    {
        return await _context.CollectionDocuments
            .Where(cd => cd.CollectionId == collectionId)
            .OrderBy(cd => cd.SortOrder)
            .ThenByDescending(cd => cd.AddedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> DocumentExistsInCollectionAsync(long collectionId, long documentId, CancellationToken cancellationToken = default)
    {
        return await _context.CollectionDocuments
            .AnyAsync(cd => cd.CollectionId == collectionId && cd.DocumentId == documentId, cancellationToken);
    }
}
