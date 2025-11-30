using TaskMaster.DocumentService.Core.Entities;

namespace TaskMaster.DocumentService.Core.Interfaces;

/// <summary>
/// Service interface for tenant management operations.
/// </summary>
public interface ITenantService
{
    /// <summary>
    /// Gets a tenant by its unique identifier.
    /// </summary>
    /// <param name="id">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The tenant if found, otherwise null.</returns>
    Task<Tenant?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a tenant by its slug.
    /// </summary>
    /// <param name="slug">The tenant slug.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The tenant if found, otherwise null.</returns>
    Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all tenants.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of all tenants.</returns>
    Task<IEnumerable<Tenant>> GetAllAsync(CancellationToken cancellationToken = default);

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

    /// <summary>
    /// Creates a new tenant.
    /// </summary>
    /// <param name="tenant">The tenant to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created tenant with assigned identifier.</returns>
    /// <exception cref="ArgumentNullException">Thrown when tenant is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a tenant with the same slug already exists.</exception>
    Task<Tenant> CreateAsync(Tenant tenant, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing tenant.
    /// </summary>
    /// <param name="tenant">The tenant to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated tenant.</returns>
    /// <exception cref="ArgumentNullException">Thrown when tenant is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant does not exist or slug conflicts with another tenant.</exception>
    Task<Tenant> UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a tenant by its identifier.
    /// </summary>
    /// <param name="id">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the tenant was deleted, false if it didn't exist.</returns>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a tenant with the specified slug exists.
    /// </summary>
    /// <param name="slug">The tenant slug.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if a tenant with the slug exists, false otherwise.</returns>
    Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates whether a tenant hierarchy is valid (no circular references).
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="parentTenantId">The proposed parent tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the hierarchy is valid, false if it would create a circular reference.</returns>
    Task<bool> ValidateHierarchyAsync(int tenantId, int? parentTenantId, CancellationToken cancellationToken = default);
}
