using TaskMaster.DocumentService.SDK.DTOs;

namespace TaskMaster.DocumentService.SDK.Interfaces;

/// <summary>
/// Client interface for tenant operations.
/// </summary>
public interface ITenantsClient
{
    /// <summary>
    /// Gets a tenant by its identifier.
    /// </summary>
    /// <param name="id">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The tenant if found.</returns>
    Task<TenantDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a tenant by its slug.
    /// </summary>
    /// <param name="slug">The tenant slug.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The tenant if found.</returns>
    Task<TenantDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all tenants.
    /// </summary>
    /// <param name="activeOnly">Whether to return only active tenants.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of tenants.</returns>
    Task<IEnumerable<TenantDto>> GetAllAsync(bool activeOnly = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets child tenants for a parent tenant.
    /// </summary>
    /// <param name="parentTenantId">The parent tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of child tenants.</returns>
    Task<IEnumerable<TenantDto>> GetChildTenantsAsync(int parentTenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new tenant.
    /// </summary>
    /// <param name="tenant">The tenant to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created tenant.</returns>
    Task<TenantDto> CreateAsync(TenantDto tenant, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing tenant.
    /// </summary>
    /// <param name="id">The tenant identifier.</param>
    /// <param name="tenant">The updated tenant.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated tenant.</returns>
    Task<TenantDto> UpdateAsync(int id, TenantDto tenant, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a tenant.
    /// </summary>
    /// <param name="id">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
