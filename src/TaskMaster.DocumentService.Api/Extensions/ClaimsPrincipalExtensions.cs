using System.Security.Claims;

namespace TaskMaster.DocumentService.Api.Extensions;

/// <summary>
/// Extension methods for <see cref="ClaimsPrincipal"/>.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Gets the tenant ID from the user's claims.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The tenant ID, or null if not found or invalid.</returns>
    public static int? GetTenantId(this ClaimsPrincipal principal)
    {
        var tenantIdClaim = principal.FindFirst("TenantId")?.Value;
        if (string.IsNullOrEmpty(tenantIdClaim))
        {
            return null;
        }

        return int.TryParse(tenantIdClaim, out var tenantId) ? tenantId : null;
    }

    /// <summary>
    /// Gets the tenant name from the user's claims.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The tenant name, or null if not found.</returns>
    public static string? GetTenantName(this ClaimsPrincipal principal)
    {
        return principal.FindFirst("TenantName")?.Value;
    }

    /// <summary>
    /// Gets the authentication type used by the user.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The authentication type (e.g., "ApiKey", "Bearer"), or null if not found.</returns>
    public static string? GetAuthenticationType(this ClaimsPrincipal principal)
    {
        return principal.FindFirst("AuthenticationType")?.Value;
    }

    /// <summary>
    /// Determines whether the user has access to the specified tenant.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <param name="tenantId">The tenant ID to check.</param>
    /// <returns>True if the user has access to the tenant; otherwise, false.</returns>
    public static bool HasAccessToTenant(this ClaimsPrincipal principal, int tenantId)
    {
        var userTenantId = principal.GetTenantId();
        return userTenantId.HasValue && userTenantId.Value == tenantId;
    }
}
