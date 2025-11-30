using TaskMaster.DocumentService.Core.Entities;

namespace TaskMaster.DocumentService.Core.Interfaces;

/// <summary>
/// Repository interface for Tenant entity operations.
/// </summary>
public interface ITenantRepository : IRepository<Tenant, int>
{
    /// <summary>
    /// Gets a tenant by its slug.
    /// </summary>
    /// <param name="slug">The tenant slug.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The tenant if found, otherwise null.</returns>
    Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active tenants.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of active tenants.</returns>
    Task<IEnumerable<Tenant>> GetActiveTenantsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets child tenants for a parent tenant.
    /// </summary>
    /// <param name="parentTenantId">The parent tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of child tenants.</returns>
    Task<IEnumerable<Tenant>> GetChildTenantsAsync(int parentTenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tenants by type.
    /// </summary>
    /// <param name="tenantType">The tenant type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of tenants of the specified type.</returns>
    Task<IEnumerable<Tenant>> GetByTypeAsync(string tenantType, CancellationToken cancellationToken = default);
}
