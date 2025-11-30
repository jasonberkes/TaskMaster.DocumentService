using Microsoft.Extensions.Logging;
using TaskMaster.DocumentService.Core.DTOs;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Repositories;

namespace TaskMaster.DocumentService.Core.Services;

/// <summary>
/// Service implementation for tenant management operations.
/// Provides business logic layer between controllers and repositories.
/// </summary>
public class TenantService : ITenantService
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ILogger<TenantService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantService"/> class.
    /// </summary>
    /// <param name="tenantRepository">The tenant repository.</param>
    /// <param name="logger">The logger instance.</param>
    public TenantService(
        ITenantRepository tenantRepository,
        ILogger<TenantService> logger)
    {
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<TenantDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting tenant by ID: {TenantId}", id);

        try
        {
            var tenant = await _tenantRepository.GetByIdAsync(id, cancellationToken);

            if (tenant == null)
            {
                _logger.LogInformation("Tenant with ID {TenantId} not found", id);
                return null;
            }

            return MapToDto(tenant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenant by ID: {TenantId}", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<TenantDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            _logger.LogWarning("GetBySlugAsync called with null or empty slug");
            throw new ArgumentException("Slug cannot be null or whitespace.", nameof(slug));
        }

        _logger.LogDebug("Getting tenant by slug: {Slug}", slug);

        try
        {
            var tenant = await _tenantRepository.GetBySlugAsync(slug, cancellationToken);

            if (tenant == null)
            {
                _logger.LogInformation("Tenant with slug '{Slug}' not found", slug);
                return null;
            }

            return MapToDto(tenant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenant by slug: {Slug}", slug);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TenantDto>> GetAllAsync(bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all tenants (activeOnly: {ActiveOnly})", activeOnly);

        try
        {
            var tenants = await _tenantRepository.GetAllAsync(activeOnly, cancellationToken);
            return tenants.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all tenants");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TenantDto>> GetChildTenantsAsync(int parentTenantId, bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting child tenants for parent ID: {ParentTenantId} (activeOnly: {ActiveOnly})", parentTenantId, activeOnly);

        try
        {
            var tenants = await _tenantRepository.GetChildTenantsAsync(parentTenantId, activeOnly, cancellationToken);
            return tenants.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving child tenants for parent ID: {ParentTenantId}", parentTenantId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TenantDto>> GetRootTenantsAsync(bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting root tenants (activeOnly: {ActiveOnly})", activeOnly);

        try
        {
            var tenants = await _tenantRepository.GetRootTenantsAsync(activeOnly, cancellationToken);
            return tenants.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving root tenants");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<TenantDto> CreateAsync(CreateTenantDto createDto, CancellationToken cancellationToken = default)
    {
        if (createDto == null)
        {
            throw new ArgumentNullException(nameof(createDto));
        }

        _logger.LogInformation("Creating new tenant with slug: {Slug}", createDto.Slug);

        // Validate required fields
        ValidateCreateDto(createDto);

        try
        {
            // Check if slug already exists
            var slugExists = await _tenantRepository.SlugExistsAsync(createDto.Slug, null, cancellationToken);
            if (slugExists)
            {
                _logger.LogWarning("Tenant creation failed: Slug '{Slug}' already exists", createDto.Slug);
                throw new InvalidOperationException($"A tenant with slug '{createDto.Slug}' already exists.");
            }

            // Validate parent tenant if specified
            if (createDto.ParentTenantId.HasValue)
            {
                var parentTenant = await _tenantRepository.GetByIdAsync(createDto.ParentTenantId.Value, cancellationToken);
                if (parentTenant == null)
                {
                    _logger.LogWarning("Tenant creation failed: Parent tenant ID {ParentTenantId} not found", createDto.ParentTenantId.Value);
                    throw new InvalidOperationException($"Parent tenant with ID {createDto.ParentTenantId.Value} not found.");
                }

                if (!parentTenant.IsActive)
                {
                    _logger.LogWarning("Tenant creation failed: Parent tenant ID {ParentTenantId} is inactive", createDto.ParentTenantId.Value);
                    throw new InvalidOperationException($"Parent tenant with ID {createDto.ParentTenantId.Value} is inactive.");
                }
            }

            // Create tenant entity
            var tenant = new Tenant
            {
                ParentTenantId = createDto.ParentTenantId,
                TenantType = createDto.TenantType,
                Name = createDto.Name,
                Slug = createDto.Slug,
                Settings = createDto.Settings,
                RetentionPolicies = createDto.RetentionPolicies,
                IsActive = true
            };

            var createdTenant = await _tenantRepository.CreateAsync(tenant, cancellationToken);

            _logger.LogInformation("Successfully created tenant with ID: {TenantId} and slug: {Slug}", createdTenant.Id, createdTenant.Slug);

            return MapToDto(createdTenant);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tenant with slug: {Slug}", createDto.Slug);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<TenantDto?> UpdateAsync(int id, UpdateTenantDto updateDto, CancellationToken cancellationToken = default)
    {
        if (updateDto == null)
        {
            throw new ArgumentNullException(nameof(updateDto));
        }

        _logger.LogInformation("Updating tenant with ID: {TenantId}", id);

        try
        {
            var tenant = await _tenantRepository.GetByIdAsync(id, cancellationToken);

            if (tenant == null)
            {
                _logger.LogInformation("Tenant with ID {TenantId} not found for update", id);
                return null;
            }

            // Update only provided fields
            if (!string.IsNullOrWhiteSpace(updateDto.Name))
            {
                tenant.Name = updateDto.Name;
            }

            if (updateDto.Settings != null)
            {
                tenant.Settings = updateDto.Settings;
            }

            if (updateDto.RetentionPolicies != null)
            {
                tenant.RetentionPolicies = updateDto.RetentionPolicies;
            }

            if (updateDto.IsActive.HasValue)
            {
                tenant.IsActive = updateDto.IsActive.Value;
            }

            var updatedTenant = await _tenantRepository.UpdateAsync(tenant, cancellationToken);

            _logger.LogInformation("Successfully updated tenant with ID: {TenantId}", id);

            return MapToDto(updatedTenant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tenant with ID: {TenantId}", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting tenant with ID: {TenantId}", id);

        try
        {
            var result = await _tenantRepository.DeleteAsync(id, cancellationToken);

            if (result)
            {
                _logger.LogInformation("Successfully deleted tenant with ID: {TenantId}", id);
            }
            else
            {
                _logger.LogInformation("Tenant with ID {TenantId} not found for deletion", id);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tenant with ID: {TenantId}", id);
            throw;
        }
    }

    /// <summary>
    /// Maps a Tenant entity to a TenantDto.
    /// </summary>
    /// <param name="tenant">The tenant entity.</param>
    /// <returns>The tenant DTO.</returns>
    private static TenantDto MapToDto(Tenant tenant)
    {
        return new TenantDto
        {
            Id = tenant.Id,
            ParentTenantId = tenant.ParentTenantId,
            TenantType = tenant.TenantType,
            Name = tenant.Name,
            Slug = tenant.Slug,
            Settings = tenant.Settings,
            RetentionPolicies = tenant.RetentionPolicies,
            CreatedAt = tenant.CreatedAt,
            UpdatedAt = tenant.UpdatedAt,
            IsActive = tenant.IsActive,
            ParentTenantName = tenant.ParentTenant?.Name
        };
    }

    /// <summary>
    /// Validates the create tenant DTO.
    /// </summary>
    /// <param name="createDto">The create DTO to validate.</param>
    /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
    private void ValidateCreateDto(CreateTenantDto createDto)
    {
        if (string.IsNullOrWhiteSpace(createDto.TenantType))
        {
            _logger.LogWarning("Tenant creation validation failed: TenantType is required");
            throw new ArgumentException("TenantType is required.", nameof(createDto));
        }

        if (string.IsNullOrWhiteSpace(createDto.Name))
        {
            _logger.LogWarning("Tenant creation validation failed: Name is required");
            throw new ArgumentException("Name is required.", nameof(createDto));
        }

        if (string.IsNullOrWhiteSpace(createDto.Slug))
        {
            _logger.LogWarning("Tenant creation validation failed: Slug is required");
            throw new ArgumentException("Slug is required.", nameof(createDto));
        }

        // Validate slug format (alphanumeric, hyphens, underscores only)
        if (!System.Text.RegularExpressions.Regex.IsMatch(createDto.Slug, @"^[a-z0-9-_]+$"))
        {
            _logger.LogWarning("Tenant creation validation failed: Invalid slug format '{Slug}'", createDto.Slug);
            throw new ArgumentException("Slug must contain only lowercase letters, numbers, hyphens, and underscores.", nameof(createDto));
        }
    }
}
