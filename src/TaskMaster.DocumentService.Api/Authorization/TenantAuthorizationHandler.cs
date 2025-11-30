using Microsoft.AspNetCore.Authorization;

namespace TaskMaster.DocumentService.Api.Authorization;

/// <summary>
/// Authorization requirement for tenant-scoped access.
/// </summary>
public class TenantAuthorizationRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantAuthorizationRequirement"/> class.
    /// </summary>
    /// <param name="requiredTenantId">The required tenant ID for authorization.</param>
    public TenantAuthorizationRequirement(int requiredTenantId)
    {
        RequiredTenantId = requiredTenantId;
    }

    /// <summary>
    /// Gets the required tenant ID.
    /// </summary>
    public int RequiredTenantId { get; }
}

/// <summary>
/// Authorization handler for tenant-scoped access validation.
/// </summary>
public class TenantAuthorizationHandler : AuthorizationHandler<TenantAuthorizationRequirement>
{
    private readonly ILogger<TenantAuthorizationHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantAuthorizationHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public TenantAuthorizationHandler(ILogger<TenantAuthorizationHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Makes a decision if authorization is allowed based on the tenant context.
    /// </summary>
    /// <param name="context">The authorization context.</param>
    /// <param name="requirement">The authorization requirement.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenantAuthorizationRequirement requirement)
    {
        // Get the tenant ID from the user's claims
        var userTenantIdClaim = context.User.FindFirst("TenantId")?.Value;

        if (string.IsNullOrEmpty(userTenantIdClaim))
        {
            _logger.LogWarning("User does not have a TenantId claim");
            return Task.CompletedTask;
        }

        if (!int.TryParse(userTenantIdClaim, out var userTenantId))
        {
            _logger.LogWarning("User's TenantId claim is not a valid integer: {TenantIdClaim}", userTenantIdClaim);
            return Task.CompletedTask;
        }

        // Check if the user's tenant ID matches the required tenant ID
        if (userTenantId == requirement.RequiredTenantId)
        {
            _logger.LogDebug(
                "Tenant authorization succeeded for user with tenant {TenantId}",
                userTenantId);
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning(
                "Tenant authorization failed. User tenant {UserTenantId} does not match required tenant {RequiredTenantId}",
                userTenantId,
                requirement.RequiredTenantId);
        }

        return Task.CompletedTask;
    }
}
