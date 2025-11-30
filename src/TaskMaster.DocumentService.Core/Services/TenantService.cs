using Microsoft.Extensions.Logging;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Interfaces;

namespace TaskMaster.DocumentService.Core.Services;

/// <summary>
/// Service implementation for tenant management operations.
/// </summary>
public class TenantService : ITenantService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TenantService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantService"/> class.
    /// </summary>
    /// <param name="unitOfWork">The unit of work.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public TenantService(
        IUnitOfWork unitOfWork,
        ILogger<TenantService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<Tenant?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ArgumentException("Tenant ID must be greater than zero.", nameof(id));

        _logger.LogDebug("Getting tenant by ID: {TenantId}", id);

        try
        {
            var tenant = await _unitOfWork.Tenants.GetByIdAsync(id, cancellationToken);

            if (tenant == null)
            {
                _logger.LogWarning("Tenant with ID {TenantId} not found", id);
            }

            return tenant;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tenant by ID: {TenantId}", id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Slug cannot be null or empty.", nameof(slug));

        _logger.LogDebug("Getting tenant by slug: {Slug}", slug);

        try
        {
            var tenant = await _unitOfWork.Tenants.GetBySlugAsync(slug, cancellationToken);

            if (tenant == null)
            {
                _logger.LogWarning("Tenant with slug {Slug} not found", slug);
            }

            return tenant;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tenant by slug: {Slug}", slug);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Tenant>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all tenants");

        try
        {
            var tenants = await _unitOfWork.Tenants.GetAllAsync(cancellationToken);
            _logger.LogInformation("Retrieved {Count} tenants", tenants.Count());
            return tenants;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all tenants");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Tenant>> GetActiveTenantsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting active tenants");

        try
        {
            var tenants = await _unitOfWork.Tenants.GetActiveTenantsAsync(cancellationToken);
            _logger.LogInformation("Retrieved {Count} active tenants", tenants.Count());
            return tenants;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active tenants");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Tenant>> GetChildTenantsAsync(int parentTenantId, CancellationToken cancellationToken = default)
    {
        if (parentTenantId <= 0)
            throw new ArgumentException("Parent tenant ID must be greater than zero.", nameof(parentTenantId));

        _logger.LogDebug("Getting child tenants for parent: {ParentTenantId}", parentTenantId);

        try
        {
            var tenants = await _unitOfWork.Tenants.GetChildTenantsAsync(parentTenantId, cancellationToken);
            _logger.LogInformation("Retrieved {Count} child tenants for parent: {ParentTenantId}",
                tenants.Count(), parentTenantId);
            return tenants;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting child tenants for parent: {ParentTenantId}", parentTenantId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Tenant>> GetByTypeAsync(string tenantType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantType))
            throw new ArgumentException("Tenant type cannot be null or empty.", nameof(tenantType));

        _logger.LogDebug("Getting tenants by type: {TenantType}", tenantType);

        try
        {
            var tenants = await _unitOfWork.Tenants.GetByTypeAsync(tenantType, cancellationToken);
            _logger.LogInformation("Retrieved {Count} tenants of type: {TenantType}",
                tenants.Count(), tenantType);
            return tenants;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tenants by type: {TenantType}", tenantType);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Tenant> CreateAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        if (tenant == null)
            throw new ArgumentNullException(nameof(tenant));

        if (string.IsNullOrWhiteSpace(tenant.Name))
            throw new ArgumentException("Tenant name cannot be null or empty.", nameof(tenant));

        if (string.IsNullOrWhiteSpace(tenant.Slug))
            throw new ArgumentException("Tenant slug cannot be null or empty.", nameof(tenant));

        if (string.IsNullOrWhiteSpace(tenant.TenantType))
            throw new ArgumentException("Tenant type cannot be null or empty.", nameof(tenant));

        _logger.LogInformation("Creating new tenant: {TenantName} with slug: {Slug}", tenant.Name, tenant.Slug);

        try
        {
            // Check if slug already exists
            var existingTenant = await _unitOfWork.Tenants.GetBySlugAsync(tenant.Slug, cancellationToken);
            if (existingTenant != null)
            {
                _logger.LogWarning("Tenant with slug {Slug} already exists", tenant.Slug);
                throw new InvalidOperationException($"A tenant with slug '{tenant.Slug}' already exists.");
            }

            // Validate parent tenant exists if specified
            if (tenant.ParentTenantId.HasValue)
            {
                var parentTenant = await _unitOfWork.Tenants.GetByIdAsync(tenant.ParentTenantId.Value, cancellationToken);
                if (parentTenant == null)
                {
                    _logger.LogWarning("Parent tenant {ParentTenantId} not found", tenant.ParentTenantId.Value);
                    throw new InvalidOperationException($"Parent tenant with ID {tenant.ParentTenantId.Value} does not exist.");
                }

                if (!parentTenant.IsActive)
                {
                    _logger.LogWarning("Parent tenant {ParentTenantId} is not active", tenant.ParentTenantId.Value);
                    throw new InvalidOperationException($"Parent tenant with ID {tenant.ParentTenantId.Value} is not active.");
                }
            }

            // Set timestamps
            tenant.CreatedAt = DateTime.UtcNow;
            tenant.UpdatedAt = null;

            var createdTenant = await _unitOfWork.Tenants.AddAsync(tenant, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully created tenant: {TenantName} with ID: {TenantId}",
                createdTenant.Name, createdTenant.Id);

            return createdTenant;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tenant: {TenantName}", tenant.Name);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Tenant> UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        if (tenant == null)
            throw new ArgumentNullException(nameof(tenant));

        if (tenant.Id <= 0)
            throw new ArgumentException("Tenant ID must be greater than zero.", nameof(tenant));

        if (string.IsNullOrWhiteSpace(tenant.Name))
            throw new ArgumentException("Tenant name cannot be null or empty.", nameof(tenant));

        if (string.IsNullOrWhiteSpace(tenant.Slug))
            throw new ArgumentException("Tenant slug cannot be null or empty.", nameof(tenant));

        if (string.IsNullOrWhiteSpace(tenant.TenantType))
            throw new ArgumentException("Tenant type cannot be null or empty.", nameof(tenant));

        _logger.LogInformation("Updating tenant: {TenantId}", tenant.Id);

        try
        {
            // Check if tenant exists
            var existingTenant = await _unitOfWork.Tenants.GetByIdAsync(tenant.Id, cancellationToken);
            if (existingTenant == null)
            {
                _logger.LogWarning("Tenant with ID {TenantId} not found", tenant.Id);
                throw new InvalidOperationException($"Tenant with ID {tenant.Id} does not exist.");
            }

            // Check if slug conflicts with another tenant
            var tenantWithSlug = await _unitOfWork.Tenants.GetBySlugAsync(tenant.Slug, cancellationToken);
            if (tenantWithSlug != null && tenantWithSlug.Id != tenant.Id)
            {
                _logger.LogWarning("Tenant with slug {Slug} already exists", tenant.Slug);
                throw new InvalidOperationException($"A tenant with slug '{tenant.Slug}' already exists.");
            }

            // Validate parent tenant if specified
            if (tenant.ParentTenantId.HasValue)
            {
                // Check for circular reference
                if (tenant.ParentTenantId.Value == tenant.Id)
                {
                    _logger.LogWarning("Tenant {TenantId} cannot be its own parent", tenant.Id);
                    throw new InvalidOperationException("A tenant cannot be its own parent.");
                }

                var isValid = await ValidateHierarchyAsync(tenant.Id, tenant.ParentTenantId, cancellationToken);
                if (!isValid)
                {
                    _logger.LogWarning("Circular reference detected for tenant {TenantId} with parent {ParentTenantId}",
                        tenant.Id, tenant.ParentTenantId.Value);
                    throw new InvalidOperationException("The specified parent would create a circular reference in the tenant hierarchy.");
                }

                var parentTenant = await _unitOfWork.Tenants.GetByIdAsync(tenant.ParentTenantId.Value, cancellationToken);
                if (parentTenant == null)
                {
                    _logger.LogWarning("Parent tenant {ParentTenantId} not found", tenant.ParentTenantId.Value);
                    throw new InvalidOperationException($"Parent tenant with ID {tenant.ParentTenantId.Value} does not exist.");
                }

                if (!parentTenant.IsActive)
                {
                    _logger.LogWarning("Parent tenant {ParentTenantId} is not active", tenant.ParentTenantId.Value);
                    throw new InvalidOperationException($"Parent tenant with ID {tenant.ParentTenantId.Value} is not active.");
                }
            }

            // Set update timestamp
            tenant.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Tenants.Update(tenant);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            var updatedTenant = tenant;

            _logger.LogInformation("Successfully updated tenant: {TenantId}", updatedTenant.Id);

            return updatedTenant;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tenant: {TenantId}", tenant.Id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ArgumentException("Tenant ID must be greater than zero.", nameof(id));

        _logger.LogInformation("Deleting tenant: {TenantId}", id);

        try
        {
            // Check if tenant exists
            var tenant = await _unitOfWork.Tenants.GetByIdAsync(id, cancellationToken);
            if (tenant == null)
            {
                _logger.LogWarning("Tenant with ID {TenantId} not found", id);
                return false;
            }

            // Check if tenant has child tenants
            var childTenants = await _unitOfWork.Tenants.GetChildTenantsAsync(id, cancellationToken);
            if (childTenants.Any())
            {
                _logger.LogWarning("Cannot delete tenant {TenantId} because it has {Count} child tenants",
                    id, childTenants.Count());
                throw new InvalidOperationException($"Cannot delete tenant {id} because it has child tenants. Delete or reassign child tenants first.");
            }

            _unitOfWork.Tenants.Remove(tenant);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully deleted tenant: {TenantId}", id);

            return true;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tenant: {TenantId}", id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Slug cannot be null or empty.", nameof(slug));

        _logger.LogDebug("Checking if tenant exists with slug: {Slug}", slug);

        try
        {
            var tenant = await _unitOfWork.Tenants.GetBySlugAsync(slug, cancellationToken);
            return tenant != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking tenant existence by slug: {Slug}", slug);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ValidateHierarchyAsync(int tenantId, int? parentTenantId, CancellationToken cancellationToken = default)
    {
        if (tenantId <= 0)
            throw new ArgumentException("Tenant ID must be greater than zero.", nameof(tenantId));

        // If no parent, hierarchy is valid
        if (!parentTenantId.HasValue)
            return true;

        // Check for self-reference
        if (parentTenantId.Value == tenantId)
            return false;

        _logger.LogDebug("Validating hierarchy for tenant {TenantId} with parent {ParentTenantId}",
            tenantId, parentTenantId.Value);

        try
        {
            // Check for circular reference by traversing up the parent chain
            var currentParentId = parentTenantId.Value;
            var visited = new HashSet<int> { tenantId };

            while (currentParentId > 0)
            {
                // If we've seen this ID before, there's a circular reference
                if (visited.Contains(currentParentId))
                {
                    _logger.LogWarning("Circular reference detected in tenant hierarchy for tenant {TenantId}", tenantId);
                    return false;
                }

                visited.Add(currentParentId);

                // Get the parent tenant
                var parent = await _unitOfWork.Tenants.GetByIdAsync(currentParentId, cancellationToken);
                if (parent == null)
                {
                    // Parent doesn't exist, but that's handled elsewhere
                    break;
                }

                // Move up to the next parent
                if (parent.ParentTenantId.HasValue)
                {
                    currentParentId = parent.ParentTenantId.Value;
                }
                else
                {
                    // Reached the root
                    break;
                }
            }

            _logger.LogDebug("Hierarchy validation successful for tenant {TenantId}", tenantId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating hierarchy for tenant {TenantId}", tenantId);
            throw;
        }
    }
}
