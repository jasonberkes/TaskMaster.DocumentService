using TaskMaster.DocumentService.Core.Entities;

namespace TaskMaster.DocumentService.Core.Repositories;

/// <summary>
/// Repository interface for tenant data access operations.
/// </summary>
public interface ITenantRepository
{
    /// <summary>
    /// Gets a tenant by its unique identifier.
    /// </summary>
    /// <param name="id">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The tenant if found; otherwise, null.</returns>
    Task<Tenant?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a tenant by its unique slug.
    /// </summary>
    /// <param name="slug">The tenant slug.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The tenant if found; otherwise, null.</returns>
    Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all tenants with optional filtering by active status.
    /// </summary>
    /// <param name="activeOnly">If true, returns only active tenants; otherwise, returns all tenants.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of tenants.</returns>
    Task<IEnumerable<Tenant>> GetAllAsync(bool activeOnly = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all child tenants for a given parent tenant.
    /// </summary>
    /// <param name="parentTenantId">The parent tenant identifier.</param>
    /// <param name="activeOnly">If true, returns only active tenants; otherwise, returns all tenants.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of child tenants.</returns>
    Task<IEnumerable<Tenant>> GetChildTenantsAsync(int parentTenantId, bool activeOnly = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all root-level tenants (tenants without a parent).
    /// </summary>
    /// <param name="activeOnly">If true, returns only active tenants; otherwise, returns all tenants.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of root-level tenants.</returns>
    Task<IEnumerable<Tenant>> GetRootTenantsAsync(bool activeOnly = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a tenant slug already exists.
    /// </summary>
    /// <param name="slug">The tenant slug to check.</param>
    /// <param name="excludeTenantId">Optional tenant ID to exclude from the check (for updates).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the slug exists; otherwise, false.</returns>
    Task<bool> SlugExistsAsync(string slug, int? excludeTenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new tenant.
    /// </summary>
    /// <param name="tenant">The tenant to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created tenant with populated ID.</returns>
    Task<Tenant> CreateAsync(Tenant tenant, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing tenant.
    /// </summary>
    /// <param name="tenant">The tenant to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated tenant.</returns>
    Task<Tenant> UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a tenant by its identifier (soft delete by setting IsActive to false).
    /// </summary>
    /// <param name="id">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the tenant was deleted; otherwise, false.</returns>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
