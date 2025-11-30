using Microsoft.EntityFrameworkCore;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Interfaces;

namespace TaskMaster.DocumentService.Data.Repositories;

/// <summary>
/// Repository implementation for Tenant entity operations.
/// </summary>
public class TenantRepository : Repository<Tenant, int>, ITenantRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public TenantRepository(DocumentServiceDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(t => t.Slug == slug, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Tenant>> GetActiveTenantsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.IsActive)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Tenant>> GetChildTenantsAsync(int parentTenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.ParentTenantId == parentTenantId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Tenant>> GetByTypeAsync(string tenantType, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.TenantType == tenantType)
            .ToListAsync(cancellationToken);
    }
}
