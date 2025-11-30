using Microsoft.EntityFrameworkCore;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Repositories;
using TaskMaster.DocumentService.Data.Data;

namespace TaskMaster.DocumentService.Data.Repositories;

/// <summary>
/// Repository implementation for tenant data access operations.
/// </summary>
public class TenantRepository : ITenantRepository
{
    private readonly DocumentServiceDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public TenantRepository(DocumentServiceDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task<Tenant?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Tenants
            .Include(t => t.ParentTenant)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Slug cannot be null or whitespace.", nameof(slug));

        return await _context.Tenants
            .Include(t => t.ParentTenant)
            .FirstOrDefaultAsync(t => t.Slug == slug, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Tenant>> GetAllAsync(bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        var query = _context.Tenants.Include(t => t.ParentTenant).AsQueryable();

        if (activeOnly)
        {
            query = query.Where(t => t.IsActive);
        }

        return await query.OrderBy(t => t.Name).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Tenant>> GetChildTenantsAsync(int parentTenantId, bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        var query = _context.Tenants
            .Include(t => t.ParentTenant)
            .Where(t => t.ParentTenantId == parentTenantId);

        if (activeOnly)
        {
            query = query.Where(t => t.IsActive);
        }

        return await query.OrderBy(t => t.Name).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Tenant>> GetRootTenantsAsync(bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        var query = _context.Tenants
            .Where(t => t.ParentTenantId == null);

        if (activeOnly)
        {
            query = query.Where(t => t.IsActive);
        }

        return await query.OrderBy(t => t.Name).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> SlugExistsAsync(string slug, int? excludeTenantId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Slug cannot be null or whitespace.", nameof(slug));

        var query = _context.Tenants.Where(t => t.Slug == slug);

        if (excludeTenantId.HasValue)
        {
            query = query.Where(t => t.Id != excludeTenantId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Tenant> CreateAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        if (tenant == null)
            throw new ArgumentNullException(nameof(tenant));

        tenant.CreatedAt = DateTime.UtcNow;
        tenant.IsActive = true;

        await _context.Tenants.AddAsync(tenant, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return tenant;
    }

    /// <inheritdoc />
    public async Task<Tenant> UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        if (tenant == null)
            throw new ArgumentNullException(nameof(tenant));

        tenant.UpdatedAt = DateTime.UtcNow;

        _context.Tenants.Update(tenant);
        await _context.SaveChangesAsync(cancellationToken);

        return tenant;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var tenant = await _context.Tenants.FindAsync(new object[] { id }, cancellationToken);

        if (tenant == null)
            return false;

        // Soft delete by setting IsActive to false
        tenant.IsActive = false;
        tenant.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
