using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Interfaces;

namespace TaskMaster.DocumentService.Data.Repositories;

/// <summary>
/// Repository implementation for document operations.
/// </summary>
public class DocumentRepository : IDocumentRepository
{
    private readonly DocumentDbContext _context;
    private readonly ILogger<DocumentRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentRepository"/> class.
    /// </summary>
    public DocumentRepository(DocumentDbContext context, ILogger<DocumentRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Document?> GetByIdAsync(Guid id, Guid tenantId)
    {
        try
        {
            return await _context.Documents
                .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId && !d.IsDeleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document {DocumentId} for tenant {TenantId}", id, tenantId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Document>> GetByTenantAsync(Guid tenantId, bool includeDeleted = false)
    {
        try
        {
            var query = _context.Documents.Where(d => d.TenantId == tenantId);

            if (!includeDeleted)
            {
                query = query.Where(d => !d.IsDeleted);
            }

            return await query.OrderByDescending(d => d.UploadedAt).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving documents for tenant {TenantId}", tenantId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Document> CreateAsync(Document document)
    {
        try
        {
            document.Id = Guid.NewGuid();
            document.UploadedAt = DateTime.UtcNow;
            document.IsDeleted = false;

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Document {DocumentId} created for tenant {TenantId}", document.Id, document.TenantId);

            return document;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating document for tenant {TenantId}", document.TenantId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Document> UpdateAsync(Document document)
    {
        try
        {
            _context.Documents.Update(document);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Document {DocumentId} updated for tenant {TenantId}", document.Id, document.TenantId);

            return document;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document {DocumentId} for tenant {TenantId}", document.Id, document.TenantId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(Guid id, Guid tenantId)
    {
        try
        {
            var document = await _context.Documents
                .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId && !d.IsDeleted);

            if (document == null)
            {
                _logger.LogWarning("Document {DocumentId} not found for tenant {TenantId}", id, tenantId);
                return false;
            }

            document.IsDeleted = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Document {DocumentId} soft deleted for tenant {TenantId}", id, tenantId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {DocumentId} for tenant {TenantId}", id, tenantId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(Guid id, Guid tenantId)
    {
        try
        {
            return await _context.Documents
                .AnyAsync(d => d.Id == id && d.TenantId == tenantId && !d.IsDeleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if document {DocumentId} exists for tenant {TenantId}", id, tenantId);
            throw;
        }
    }
}
