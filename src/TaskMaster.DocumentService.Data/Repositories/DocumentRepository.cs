using Microsoft.EntityFrameworkCore;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Interfaces;
using TaskMaster.DocumentService.Data.Context;

namespace TaskMaster.DocumentService.Data.Repositories;

/// <summary>
/// Repository implementation for document data access operations
/// </summary>
public class DocumentRepository : IDocumentRepository
{
    private readonly DocumentServiceDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentRepository"/> class
    /// </summary>
    /// <param name="context">The database context</param>
    public DocumentRepository(DocumentServiceDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc/>
    public async Task<Document?> GetByIdAsync(long id, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Documents
            .Include(d => d.Tenant)
            .Include(d => d.DocumentType)
            .AsQueryable();

        if (!includeDeleted)
        {
            query = query.Where(d => !d.IsDeleted);
        }

        return await query.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Document>> GetByTenantIdAsync(int tenantId, int skip = 0, int take = 50, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Documents
            .Include(d => d.DocumentType)
            .Where(d => d.TenantId == tenantId);

        if (!includeDeleted)
        {
            query = query.Where(d => !d.IsDeleted);
        }

        return await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Document>> GetVersionsAsync(long parentDocumentId, CancellationToken cancellationToken = default)
    {
        return await _context.Documents
            .Where(d => d.ParentDocumentId == parentDocumentId || d.Id == parentDocumentId)
            .OrderBy(d => d.Version)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Document?> GetCurrentVersionAsync(long parentDocumentId, CancellationToken cancellationToken = default)
    {
        return await _context.Documents
            .Where(d => (d.ParentDocumentId == parentDocumentId || d.Id == parentDocumentId) && d.IsCurrentVersion)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsByContentHashAsync(int tenantId, string contentHash, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(contentHash))
        {
            return false;
        }

        return await _context.Documents
            .AnyAsync(d => d.TenantId == tenantId && d.ContentHash == contentHash && !d.IsDeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Document> CreateAsync(Document document, CancellationToken cancellationToken = default)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        document.CreatedAt = DateTime.UtcNow;
        document.Version = 1;
        document.IsCurrentVersion = true;
        document.IsDeleted = false;
        document.IsArchived = false;

        _context.Documents.Add(document);
        await _context.SaveChangesAsync(cancellationToken);

        return document;
    }

    /// <inheritdoc/>
    public async Task<Document> UpdateAsync(Document document, CancellationToken cancellationToken = default)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        document.UpdatedAt = DateTime.UtcNow;

        _context.Documents.Update(document);
        await _context.SaveChangesAsync(cancellationToken);

        return document;
    }

    /// <inheritdoc/>
    public async Task<bool> SoftDeleteAsync(long id, string? deletedBy, string? deletedReason, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.FindAsync(new object[] { id }, cancellationToken);
        if (document == null)
        {
            return false;
        }

        document.IsDeleted = true;
        document.DeletedAt = DateTime.UtcNow;
        document.DeletedBy = deletedBy;
        document.DeletedReason = deletedReason;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> ArchiveAsync(long id, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.FindAsync(new object[] { id }, cancellationToken);
        if (document == null)
        {
            return false;
        }

        document.IsArchived = true;
        document.ArchivedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> UnarchiveAsync(long id, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.FindAsync(new object[] { id }, cancellationToken);
        if (document == null)
        {
            return false;
        }

        document.IsArchived = false;
        document.ArchivedAt = null;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc/>
    public async Task<int> GetCountByTenantIdAsync(int tenantId, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Documents.Where(d => d.TenantId == tenantId);

        if (!includeDeleted)
        {
            query = query.Where(d => !d.IsDeleted);
        }

        return await query.CountAsync(cancellationToken);
    }
}
