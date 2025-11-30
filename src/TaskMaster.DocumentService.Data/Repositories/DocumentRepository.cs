using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskMaster.DocumentService.Core.Entities;

namespace TaskMaster.DocumentService.Data.Repositories;

/// <summary>
/// Repository for document operations.
/// </summary>
public class DocumentRepository : IDocumentRepository
{
    private readonly DocumentServiceDbContext _context;
    private readonly ILogger<DocumentRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger instance.</param>
    public DocumentRepository(DocumentServiceDbContext context, ILogger<DocumentRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Document?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.Documents
            .Include(d => d.DocumentType)
            .Include(d => d.Tenant)
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<Document>> GetByTenantIdAsync(int tenantId, bool includeArchived = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Documents
            .Include(d => d.DocumentType)
            .Where(d => d.TenantId == tenantId && !d.IsDeleted);

        if (!includeArchived)
        {
            query = query.Where(d => !d.IsArchived);
        }

        return await query.ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<Document>> GetByDocumentTypeIdAsync(int documentTypeId, CancellationToken cancellationToken = default)
    {
        return await _context.Documents
            .Include(d => d.DocumentType)
            .Include(d => d.Tenant)
            .Where(d => d.DocumentTypeId == documentTypeId && !d.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<Document>> GetUnindexedDocumentsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Documents
            .Include(d => d.DocumentType)
            .Include(d => d.Tenant)
            .Where(d => !d.IsDeleted &&
                       (d.MeilisearchId == null ||
                        d.LastIndexedAt == null ||
                        d.UpdatedAt > d.LastIndexedAt))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Document> CreateAsync(Document document, CancellationToken cancellationToken = default)
    {
        document.CreatedAt = DateTime.UtcNow;
        _context.Documents.Add(document);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created document {DocumentId} for tenant {TenantId}", document.Id, document.TenantId);

        return document;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Document document, CancellationToken cancellationToken = default)
    {
        document.UpdatedAt = DateTime.UtcNow;
        _context.Documents.Update(document);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated document {DocumentId}", document.Id);
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(long id, string deletedBy, string? reason = null, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.FindAsync(new object[] { id }, cancellationToken);
        if (document == null || document.IsDeleted)
        {
            return false;
        }

        document.IsDeleted = true;
        document.DeletedAt = DateTime.UtcNow;
        document.DeletedBy = deletedBy;
        document.DeletedReason = reason;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted document {DocumentId} by {DeletedBy}", id, deletedBy);

        return true;
    }

    /// <inheritdoc/>
    public async Task UpdateIndexingInfoAsync(long documentId, string meilisearchId, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.FindAsync(new object[] { documentId }, cancellationToken);
        if (document == null)
        {
            throw new InvalidOperationException($"Document {documentId} not found");
        }

        document.MeilisearchId = meilisearchId;
        document.LastIndexedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated indexing info for document {DocumentId}", documentId);
    }
}
