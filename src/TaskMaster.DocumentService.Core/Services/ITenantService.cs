using TaskMaster.DocumentService.Core.DTOs;

namespace TaskMaster.DocumentService.Core.Services;

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
    /// <returns>The tenant DTO if found; otherwise, null.</returns>
    Task<TenantDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a tenant by its unique slug.
    /// </summary>
    /// <param name="slug">The tenant slug.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The tenant DTO if found; otherwise, null.</returns>
    Task<TenantDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all tenants with optional filtering.
    /// </summary>
    /// <param name="activeOnly">If true, returns only active tenants.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of tenant DTOs.</returns>
    Task<IEnumerable<TenantDto>> GetAllAsync(bool activeOnly = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all child tenants for a given parent tenant.
    /// </summary>
    /// <param name="parentTenantId">The parent tenant identifier.</param>
    /// <param name="activeOnly">If true, returns only active tenants.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of child tenant DTOs.</returns>
    Task<IEnumerable<TenantDto>> GetChildTenantsAsync(int parentTenantId, bool activeOnly = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all root-level tenants.
    /// </summary>
    /// <param name="activeOnly">If true, returns only active tenants.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of root-level tenant DTOs.</returns>
    Task<IEnumerable<TenantDto>> GetRootTenantsAsync(bool activeOnly = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new tenant.
    /// </summary>
    /// <param name="createDto">The tenant creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created tenant DTO.</returns>
    /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the slug already exists or parent tenant is invalid.</exception>
    Task<TenantDto> CreateAsync(CreateTenantDto createDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing tenant.
    /// </summary>
    /// <param name="id">The tenant identifier.</param>
    /// <param name="updateDto">The tenant update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated tenant DTO if found; otherwise, null.</returns>
    /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
    Task<TenantDto?> UpdateAsync(int id, UpdateTenantDto updateDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a tenant (soft delete).
    /// </summary>
    /// <param name="id">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the tenant was deleted; otherwise, false.</returns>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
