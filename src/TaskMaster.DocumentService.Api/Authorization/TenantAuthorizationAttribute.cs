using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TaskMaster.DocumentService.Api.Authorization;

/// <summary>
/// Authorization attribute that validates tenant-scoped access to resources.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class TenantAuthorizationAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string _tenantIdParameterName;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantAuthorizationAttribute"/> class.
    /// </summary>
    /// <param name="tenantIdParameterName">The name of the route/query parameter containing the tenant ID. Defaults to "tenantId".</param>
    public TenantAuthorizationAttribute(string tenantIdParameterName = "tenantId")
    {
        _tenantIdParameterName = tenantIdParameterName;
    }

    /// <summary>
    /// Called when a process requests authorization.
    /// </summary>
    /// <param name="context">The authorization filter context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Check if user is authenticated
        if (context.HttpContext.User?.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return Task.CompletedTask;
        }

        // Get the tenant ID from the user's claims
        var userTenantIdClaim = context.HttpContext.User.FindFirst("TenantId")?.Value;
        if (string.IsNullOrEmpty(userTenantIdClaim) || !int.TryParse(userTenantIdClaim, out var userTenantId))
        {
            context.Result = new ForbidResult();
            return Task.CompletedTask;
        }

        // Get the requested tenant ID from route data or query string
        int? requestedTenantId = null;

        // Try to get from route data first
        if (context.RouteData.Values.TryGetValue(_tenantIdParameterName, out var routeTenantId))
        {
            if (routeTenantId is int intValue)
            {
                requestedTenantId = intValue;
            }
            else if (int.TryParse(routeTenantId?.ToString(), out var parsedValue))
            {
                requestedTenantId = parsedValue;
            }
        }

        // If not in route, try query string
        if (!requestedTenantId.HasValue)
        {
            var queryTenantId = context.HttpContext.Request.Query[_tenantIdParameterName].FirstOrDefault();
            if (!string.IsNullOrEmpty(queryTenantId) && int.TryParse(queryTenantId, out var parsedQueryValue))
            {
                requestedTenantId = parsedQueryValue;
            }
        }

        // If no tenant ID in request, deny access
        if (!requestedTenantId.HasValue)
        {
            context.Result = new BadRequestObjectResult(new
            {
                error = "TenantIdRequired",
                message = $"The '{_tenantIdParameterName}' parameter is required."
            });
            return Task.CompletedTask;
        }

        // Validate that the user's tenant ID matches the requested tenant ID
        if (userTenantId != requestedTenantId.Value)
        {
            var logger = context.HttpContext.RequestServices
                .GetService<ILogger<TenantAuthorizationAttribute>>();

            logger?.LogWarning(
                "Tenant authorization failed. User tenant {UserTenantId} attempted to access resources for tenant {RequestedTenantId}",
                userTenantId,
                requestedTenantId.Value);

            context.Result = new ForbidResult();
            return Task.CompletedTask;
        }

        // Authorization successful
        return Task.CompletedTask;
    }
}
