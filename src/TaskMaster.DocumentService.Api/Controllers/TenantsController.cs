using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskMaster.DocumentService.Api.Authorization;
using TaskMaster.DocumentService.Api.Extensions;
using TaskMaster.DocumentService.Core.Interfaces;

namespace TaskMaster.DocumentService.Api.Controllers;

/// <summary>
/// Controller for tenant-related operations.
/// Demonstrates JWT and API key authentication with tenant-scoped authorization.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // Requires either JWT or API Key authentication
public class TenantsController : ControllerBase
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ILogger<TenantsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantsController"/> class.
    /// </summary>
    /// <param name="tenantRepository">The tenant repository.</param>
    /// <param name="logger">The logger.</param>
    public TenantsController(
        ITenantRepository tenantRepository,
        ILogger<TenantsController> logger)
    {
        _tenantRepository = tenantRepository;
        _logger = logger;
    }

    /// <summary>
    /// Gets tenant information by tenant ID.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <returns>The tenant information.</returns>
    /// <response code="200">Returns the tenant information.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user does not have access to the specified tenant.</response>
    /// <response code="404">If the tenant is not found.</response>
    [HttpGet("{tenantId}")]
    [TenantAuthorization("tenantId")] // Validates tenant-scoped access
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTenant(int tenantId)
    {
        _logger.LogInformation(
            "User {UserId} from tenant {UserTenantId} requesting tenant {RequestedTenantId}",
            User.Identity?.Name,
            User.GetTenantId(),
            tenantId);

        var tenant = await _tenantRepository.GetByIdAsync(tenantId);
        if (tenant == null)
        {
            _logger.LogWarning("Tenant {TenantId} not found", tenantId);
            return NotFound(new { error = "TenantNotFound", message = $"Tenant with ID {tenantId} not found." });
        }

        return Ok(new
        {
            id = tenant.Id,
            name = tenant.Name,
            slug = tenant.Slug,
            tenantType = tenant.TenantType,
            isActive = tenant.IsActive,
            createdAt = tenant.CreatedAt
        });
    }

    /// <summary>
    /// Gets the current authenticated user's tenant information.
    /// </summary>
    /// <returns>The current user's tenant information.</returns>
    /// <response code="200">Returns the current user's tenant information.</response>
    /// <response code="401">If the user is not authenticated.</response>
    [HttpGet("current")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetCurrentTenant()
    {
        var tenantId = User.GetTenantId();
        var tenantName = User.GetTenantName();
        var authenticationType = User.GetAuthenticationType();

        _logger.LogInformation(
            "User {UserId} requesting current tenant information",
            User.Identity?.Name);

        return Ok(new
        {
            tenantId,
            tenantName,
            authenticationType,
            userId = User.Identity?.Name,
            isAuthenticated = User.Identity?.IsAuthenticated ?? false
        });
    }

    /// <summary>
    /// Lists child tenants for a given parent tenant.
    /// </summary>
    /// <param name="tenantId">The parent tenant ID.</param>
    /// <returns>A list of child tenants.</returns>
    /// <response code="200">Returns the list of child tenants.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user does not have access to the specified tenant.</response>
    [HttpGet("{tenantId}/children")]
    [TenantAuthorization("tenantId")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetChildTenants(int tenantId)
    {
        _logger.LogInformation(
            "User {UserId} requesting child tenants for tenant {TenantId}",
            User.Identity?.Name,
            tenantId);

        var childTenants = await _tenantRepository.GetChildTenantsAsync(tenantId);

        return Ok(childTenants.Select(t => new
        {
            id = t.Id,
            name = t.Name,
            slug = t.Slug,
            tenantType = t.TenantType,
            isActive = t.IsActive
        }));
    }
}
