using Microsoft.EntityFrameworkCore;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Interfaces;

namespace TaskMaster.DocumentService.Data.Repositories;

/// <summary>
/// Repository implementation for DocumentTemplate entity operations.
/// </summary>
public class DocumentTemplateRepository : Repository<DocumentTemplate, long>, IDocumentTemplateRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentTemplateRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public DocumentTemplateRepository(DocumentServiceDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DocumentTemplate>> GetByTenantIdAsync(
        int tenantId,
        bool includeDeleted = false,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(t => t.TenantId == tenantId);

        if (!includeDeleted)
        {
            query = query.Where(t => !t.IsDeleted);
        }

        if (!includeInactive)
        {
            query = query.Where(t => t.IsActive);
        }

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DocumentTemplate>> GetByDocumentTypeIdAsync(
        int documentTypeId,
        bool includeDeleted = false,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(t => t.DocumentTypeId == documentTypeId);

        if (!includeDeleted)
        {
            query = query.Where(t => !t.IsDeleted);
        }

        if (!includeInactive)
        {
            query = query.Where(t => t.IsActive);
        }

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DocumentTemplate>> GetByCategoryAsync(
        int tenantId,
        string category,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.TenantId == tenantId &&
                       t.Category == category &&
                       !t.IsDeleted &&
                       t.IsActive)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<DocumentTemplate?> GetCurrentVersionAsync(long parentTemplateId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.ParentTemplateId == parentTemplateId && t.IsCurrentVersion && !t.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DocumentTemplate>> GetVersionsAsync(long parentTemplateId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.ParentTemplateId == parentTemplateId)
            .OrderByDescending(t => t.Version)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DocumentTemplate>> GetActiveTemplatesAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.TenantId == tenantId && t.IsActive && !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task SoftDeleteAsync(long templateId, string deletedBy, CancellationToken cancellationToken = default)
    {
        var template = await GetByIdAsync(templateId, cancellationToken);
        if (template == null)
        {
            throw new InvalidOperationException($"Template with ID {templateId} not found.");
        }

        template.IsDeleted = true;
        template.DeletedAt = DateTime.UtcNow;
        template.DeletedBy = deletedBy;
        template.IsActive = false; // Deactivate when deleted

        Update(template);
    }

    /// <inheritdoc/>
    public async Task RestoreAsync(long templateId, CancellationToken cancellationToken = default)
    {
        var template = await _dbSet
            .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);

        if (template == null)
        {
            throw new InvalidOperationException($"Template with ID {templateId} not found.");
        }

        template.IsDeleted = false;
        template.DeletedAt = null;
        template.DeletedBy = null;
        // Note: Does not automatically reactivate; IsActive must be set separately if desired

        Update(template);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DocumentTemplate>> SearchByNameAsync(
        int tenantId,
        string searchTerm,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.TenantId == tenantId &&
                       !t.IsDeleted &&
                       t.IsActive &&
                       t.Name.Contains(searchTerm))
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
