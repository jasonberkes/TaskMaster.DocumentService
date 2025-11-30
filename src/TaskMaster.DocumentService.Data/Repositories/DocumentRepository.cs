using Microsoft.EntityFrameworkCore;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Interfaces;

namespace TaskMaster.DocumentService.Data.Repositories;

/// <summary>
/// Repository implementation for Document entity operations.
/// </summary>
public class DocumentRepository : Repository<Document, long>, IDocumentRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public DocumentRepository(DocumentServiceDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Document>> GetByTenantIdAsync(int tenantId, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(d => d.TenantId == tenantId);

        if (!includeDeleted)
        {
            query = query.Where(d => !d.IsDeleted);
        }

        return await query.ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Document>> GetByDocumentTypeIdAsync(int documentTypeId, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(d => d.DocumentTypeId == documentTypeId);

        if (!includeDeleted)
        {
            query = query.Where(d => !d.IsDeleted);
        }

        return await query.ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Document?> GetCurrentVersionAsync(long parentDocumentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(d => d.ParentDocumentId == parentDocumentId && d.IsCurrentVersion && !d.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Document>> GetVersionsAsync(long parentDocumentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(d => d.ParentDocumentId == parentDocumentId)
            .OrderByDescending(d => d.Version)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Document>> GetByContentHashAsync(string contentHash, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(d => d.ContentHash == contentHash && !d.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task SoftDeleteAsync(long documentId, string deletedBy, string? deletedReason = null, CancellationToken cancellationToken = default)
    {
        var document = await GetByIdAsync(documentId, cancellationToken);
        if (document == null)
        {
            throw new InvalidOperationException($"Document with ID {documentId} not found.");
        }

        document.IsDeleted = true;
        document.DeletedAt = DateTime.UtcNow;
        document.DeletedBy = deletedBy;
        document.DeletedReason = deletedReason;

        Update(document);
    }

    /// <inheritdoc/>
    public async Task RestoreAsync(long documentId, CancellationToken cancellationToken = default)
    {
        var document = await _dbSet
            .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);

        if (document == null)
        {
            throw new InvalidOperationException($"Document with ID {documentId} not found.");
        }

        document.IsDeleted = false;
        document.DeletedAt = null;
        document.DeletedBy = null;
        document.DeletedReason = null;

        Update(document);
    }

    /// <inheritdoc/>
    public async Task ArchiveAsync(long documentId, CancellationToken cancellationToken = default)
    {
        var document = await GetByIdAsync(documentId, cancellationToken);
        if (document == null)
        {
            throw new InvalidOperationException($"Document with ID {documentId} not found.");
        }

        document.IsArchived = true;
        document.ArchivedAt = DateTime.UtcNow;

        Update(document);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Document>> GetArchivedDocumentsAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(d => d.TenantId == tenantId && d.IsArchived && !d.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Document>> GetDocumentsNeedingIndexingAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(d => d.DocumentType)
            .Where(d => d.DocumentType.IsContentIndexed &&
                       !d.IsDeleted &&
                       (d.LastIndexedAt == null || d.UpdatedAt > d.LastIndexedAt))
            .ToListAsync(cancellationToken);
    }
}
