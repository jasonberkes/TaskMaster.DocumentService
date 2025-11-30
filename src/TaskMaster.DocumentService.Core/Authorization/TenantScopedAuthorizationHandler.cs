using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace TaskMaster.DocumentService.Core.Authorization;

/// <summary>
/// Authorization requirement for tenant-scoped access.
/// </summary>
public class TenantScopedRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Gets or sets a value indicating whether tenant access is required.
    /// </summary>
    public bool RequireTenantAccess { get; set; } = true;
}

/// <summary>
/// Authorization handler for tenant-scoped access.
/// </summary>
public class TenantScopedAuthorizationHandler : AuthorizationHandler<TenantScopedRequirement>
{
    /// <summary>
    /// Makes a decision if authorization is allowed based on tenant context.
    /// </summary>
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenantScopedRequirement requirement)
    {
        if (!requirement.RequireTenantAccess)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Check if user has a tenant claim
        var tenantClaim = context.User.FindFirst("TenantId");
        if (tenantClaim == null)
        {
            // No tenant claim, fail the requirement
            return Task.CompletedTask;
        }

        // If we have a tenant claim and it's a valid GUID, succeed
        if (Guid.TryParse(tenantClaim.Value, out _))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Policy names for authorization.
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>
    /// Policy name for tenant-scoped access.
    /// </summary>
    public const string TenantScoped = "TenantScoped";
}

/// <summary>
/// Extension methods for claims principal to extract tenant information.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Gets the tenant ID from the claims principal.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The tenant ID, or null if not found.</returns>
    public static Guid? GetTenantId(this ClaimsPrincipal principal)
    {
        var tenantClaim = principal.FindFirst("TenantId");
        if (tenantClaim != null && Guid.TryParse(tenantClaim.Value, out var tenantId))
        {
            return tenantId;
        }
        return null;
    }

    /// <summary>
    /// Gets the user ID from the claims principal.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The user ID, or an empty string if not found.</returns>
    public static string GetUserId(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("sub")?.Value
            ?? principal.FindFirst(ClaimTypes.Name)?.Value
            ?? string.Empty;
    }
}
