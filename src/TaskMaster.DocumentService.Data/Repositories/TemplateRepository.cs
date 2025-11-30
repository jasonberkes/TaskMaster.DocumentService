using Microsoft.EntityFrameworkCore;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Interfaces;
using TaskMaster.DocumentService.Data.Context;

namespace TaskMaster.DocumentService.Data.Repositories;

/// <summary>
/// Repository implementation for managing document templates.
/// </summary>
public class TemplateRepository : ITemplateRepository
{
    private readonly DocumentServiceDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="TemplateRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public TemplateRepository(DocumentServiceDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task<DocumentTemplate?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.DocumentTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<DocumentTemplate>> GetByTenantIdAsync(int tenantId, bool includePublic = true, CancellationToken cancellationToken = default)
    {
        var query = _context.DocumentTemplates
            .AsNoTracking()
            .Where(t => !t.IsDeleted && t.IsActive && t.IsCurrentVersion);

        if (includePublic)
        {
            query = query.Where(t => t.TenantId == tenantId || t.IsPublic);
        }
        else
        {
            query = query.Where(t => t.TenantId == tenantId);
        }

        return await query
            .OrderBy(t => t.Category)
            .ThenBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<DocumentTemplate>> GetByTypeAsync(int tenantId, string templateType, bool includePublic = true, CancellationToken cancellationToken = default)
    {
        var query = _context.DocumentTemplates
            .AsNoTracking()
            .Where(t => !t.IsDeleted && t.IsActive && t.IsCurrentVersion && t.TemplateType == templateType);

        if (includePublic)
        {
            query = query.Where(t => t.TenantId == tenantId || t.IsPublic);
        }
        else
        {
            query = query.Where(t => t.TenantId == tenantId);
        }

        return await query
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<DocumentTemplate>> SearchByNameAsync(int tenantId, string searchTerm, CancellationToken cancellationToken = default)
    {
        return await _context.DocumentTemplates
            .AsNoTracking()
            .Where(t => !t.IsDeleted && t.IsActive && t.IsCurrentVersion &&
                       (t.TenantId == tenantId || t.IsPublic) &&
                       t.Name.Contains(searchTerm))
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DocumentTemplate> CreateAsync(DocumentTemplate template, CancellationToken cancellationToken = default)
    {
        template.CreatedAt = DateTime.UtcNow;
        template.IsDeleted = false;
        template.IsActive = true;

        await _context.DocumentTemplates.AddAsync(template, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return template;
    }

    /// <inheritdoc />
    public async Task<DocumentTemplate> UpdateAsync(DocumentTemplate template, CancellationToken cancellationToken = default)
    {
        template.UpdatedAt = DateTime.UtcNow;

        _context.DocumentTemplates.Update(template);
        await _context.SaveChangesAsync(cancellationToken);

        return template;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int id, string deletedBy, CancellationToken cancellationToken = default)
    {
        var template = await _context.DocumentTemplates
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, cancellationToken);

        if (template == null)
        {
            return false;
        }

        template.IsDeleted = true;
        template.DeletedAt = DateTime.UtcNow;
        template.DeletedBy = deletedBy;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<TemplateUsageLog> LogUsageAsync(TemplateUsageLog usageLog, CancellationToken cancellationToken = default)
    {
        usageLog.UsedAt = DateTime.UtcNow;

        await _context.TemplateUsageLog.AddAsync(usageLog, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return usageLog;
    }

    /// <inheritdoc />
    public async Task<List<TemplateUsageLog>> GetUsageStatisticsAsync(int templateId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var query = _context.TemplateUsageLog
            .AsNoTracking()
            .Where(l => l.TemplateId == templateId);

        if (startDate.HasValue)
        {
            query = query.Where(l => l.UsedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(l => l.UsedAt <= endDate.Value);
        }

        return await query
            .OrderByDescending(l => l.UsedAt)
            .ToListAsync(cancellationToken);
    }
}
