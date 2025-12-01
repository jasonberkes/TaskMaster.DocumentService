using Microsoft.EntityFrameworkCore;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Interfaces;

namespace TaskMaster.DocumentService.Data.Repositories;

/// <summary>
/// Repository implementation for document template operations.
/// </summary>
public class TemplateRepository : Repository<DocumentTemplate, int>, ITemplateRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TemplateRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public TemplateRepository(DocumentServiceDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DocumentTemplate>> GetByTenantIdAsync(int tenantId, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(t => t.Variables.OrderBy(v => v.SortOrder))
            .Where(t => t.TenantId == tenantId);

        if (!includeDeleted)
        {
            query = query.Where(t => !t.IsDeleted);
        }

        return await query
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<DocumentTemplate?> GetByTenantAndNameAsync(int tenantId, string templateName, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.Variables.OrderBy(v => v.SortOrder))
            .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.Name == templateName && !t.IsDeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DocumentTemplate>> GetByDocumentTypeIdAsync(int documentTypeId, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(t => t.Variables.OrderBy(v => v.SortOrder))
            .Where(t => t.DocumentTypeId == documentTypeId);

        if (!includeDeleted)
        {
            query = query.Where(t => !t.IsDeleted);
        }

        return await query
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DocumentTemplate>> GetActiveTemplatesAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.Variables.OrderBy(v => v.SortOrder))
            .Where(t => t.TenantId == tenantId && t.IsActive && !t.IsDeleted)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<DocumentTemplate?> GetTemplateWithVariablesAsync(int templateId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.Variables.OrderBy(v => v.SortOrder))
            .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task SoftDeleteAsync(int templateId, string deletedBy, string? deletedReason = null, CancellationToken cancellationToken = default)
    {
        var template = await _dbSet.FindAsync(new object[] { templateId }, cancellationToken);
        if (template != null)
        {
            template.IsDeleted = true;
            template.DeletedAt = DateTime.UtcNow;
            template.DeletedBy = deletedBy;
            template.DeletedReason = deletedReason;
            _dbSet.Update(template);
        }
    }

    /// <inheritdoc/>
    public async Task RestoreAsync(int templateId, CancellationToken cancellationToken = default)
    {
        var template = await _dbSet.FindAsync(new object[] { templateId }, cancellationToken);
        if (template != null)
        {
            template.IsDeleted = false;
            template.DeletedAt = null;
            template.DeletedBy = null;
            template.DeletedReason = null;
            _dbSet.Update(template);
        }
    }
}
