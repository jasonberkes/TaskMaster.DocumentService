using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using TaskMaster.DocumentService.Api.Authorization;
using Xunit;

namespace TaskMaster.DocumentService.Api.Tests.Authorization;

/// <summary>
/// Unit tests for <see cref="TenantAuthorizationAttribute"/>.
/// </summary>
public class TenantAuthorizationAttributeTests
{
    private readonly Mock<ILogger<TenantAuthorizationAttribute>> _logger;
    private readonly DefaultHttpContext _httpContext;
    private readonly ActionContext _actionContext;

    public TenantAuthorizationAttributeTests()
    {
        _logger = new Mock<ILogger<TenantAuthorizationAttribute>>();
        _httpContext = new DefaultHttpContext();

        // Setup service provider with logger
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(_logger.Object);
        _httpContext.RequestServices = serviceCollection.BuildServiceProvider();

        _actionContext = new ActionContext(
            _httpContext,
            new RouteData(),
            new ActionDescriptor());
    }

    /// <summary>
    /// Test that authorization succeeds when user's tenant ID matches the route parameter.
    /// </summary>
    [Fact]
    public async Task OnAuthorizationAsync_WithMatchingTenantIdInRoute_Succeeds()
    {
        // Arrange
        var tenantId = 1;
        var claims = new List<Claim>
        {
            new("TenantId", tenantId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _httpContext.User = new ClaimsPrincipal(identity);

        var routeData = new RouteData();
        routeData.Values["tenantId"] = tenantId;

        var actionContext = new ActionContext(
            _httpContext,
            routeData,
            new ActionDescriptor());

        var authContext = new AuthorizationFilterContext(
            actionContext,
            new List<IFilterMetadata>());

        var attribute = new TenantAuthorizationAttribute();

        // Act
        await attribute.OnAuthorizationAsync(authContext);

        // Assert
        Assert.Null(authContext.Result);
    }

    /// <summary>
    /// Test that authorization succeeds when user's tenant ID matches the query parameter.
    /// </summary>
    [Fact]
    public async Task OnAuthorizationAsync_WithMatchingTenantIdInQuery_Succeeds()
    {
        // Arrange
        var tenantId = 1;
        var claims = new List<Claim>
        {
            new("TenantId", tenantId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _httpContext.User = new ClaimsPrincipal(identity);
        _httpContext.Request.QueryString = new QueryString($"?tenantId={tenantId}");

        var authContext = new AuthorizationFilterContext(
            _actionContext,
            new List<IFilterMetadata>());

        var attribute = new TenantAuthorizationAttribute();

        // Act
        await attribute.OnAuthorizationAsync(authContext);

        // Assert
        Assert.Null(authContext.Result);
    }

    /// <summary>
    /// Test that authorization fails when user is not authenticated.
    /// </summary>
    [Fact]
    public async Task OnAuthorizationAsync_WithUnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity()); // Not authenticated

        var authContext = new AuthorizationFilterContext(
            _actionContext,
            new List<IFilterMetadata>());

        var attribute = new TenantAuthorizationAttribute();

        // Act
        await attribute.OnAuthorizationAsync(authContext);

        // Assert
        Assert.IsType<UnauthorizedResult>(authContext.Result);
    }

    /// <summary>
    /// Test that authorization fails when user does not have a TenantId claim.
    /// </summary>
    [Fact]
    public async Task OnAuthorizationAsync_WithoutTenantIdClaim_ReturnsForbid()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "TestUser")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _httpContext.User = new ClaimsPrincipal(identity);

        var authContext = new AuthorizationFilterContext(
            _actionContext,
            new List<IFilterMetadata>());

        var attribute = new TenantAuthorizationAttribute();

        // Act
        await attribute.OnAuthorizationAsync(authContext);

        // Assert
        Assert.IsType<ForbidResult>(authContext.Result);
    }

    /// <summary>
    /// Test that authorization fails when tenant ID parameter is missing.
    /// </summary>
    [Fact]
    public async Task OnAuthorizationAsync_WithoutTenantIdParameter_ReturnsBadRequest()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("TenantId", "1")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _httpContext.User = new ClaimsPrincipal(identity);

        var authContext = new AuthorizationFilterContext(
            _actionContext,
            new List<IFilterMetadata>());

        var attribute = new TenantAuthorizationAttribute();

        // Act
        await attribute.OnAuthorizationAsync(authContext);

        // Assert
        Assert.IsType<BadRequestObjectResult>(authContext.Result);
    }

    /// <summary>
    /// Test that authorization fails when user's tenant ID does not match the requested tenant ID.
    /// </summary>
    [Fact]
    public async Task OnAuthorizationAsync_WithNonMatchingTenantId_ReturnsForbid()
    {
        // Arrange
        var userTenantId = 1;
        var requestedTenantId = 2;
        var claims = new List<Claim>
        {
            new("TenantId", userTenantId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _httpContext.User = new ClaimsPrincipal(identity);

        var routeData = new RouteData();
        routeData.Values["tenantId"] = requestedTenantId;

        var actionContext = new ActionContext(
            _httpContext,
            routeData,
            new ActionDescriptor());

        var authContext = new AuthorizationFilterContext(
            actionContext,
            new List<IFilterMetadata>());

        var attribute = new TenantAuthorizationAttribute();

        // Act
        await attribute.OnAuthorizationAsync(authContext);

        // Assert
        Assert.IsType<ForbidResult>(authContext.Result);
    }

    /// <summary>
    /// Test that custom parameter name is respected.
    /// </summary>
    [Fact]
    public async Task OnAuthorizationAsync_WithCustomParameterName_Succeeds()
    {
        // Arrange
        var tenantId = 1;
        var claims = new List<Claim>
        {
            new("TenantId", tenantId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _httpContext.User = new ClaimsPrincipal(identity);

        var routeData = new RouteData();
        routeData.Values["customTenantId"] = tenantId;

        var actionContext = new ActionContext(
            _httpContext,
            routeData,
            new ActionDescriptor());

        var authContext = new AuthorizationFilterContext(
            actionContext,
            new List<IFilterMetadata>());

        var attribute = new TenantAuthorizationAttribute("customTenantId");

        // Act
        await attribute.OnAuthorizationAsync(authContext);

        // Assert
        Assert.Null(authContext.Result);
    }

    /// <summary>
    /// Test that route data takes precedence over query string.
    /// </summary>
    [Fact]
    public async Task OnAuthorizationAsync_RouteDataTakesPrecedenceOverQueryString_Succeeds()
    {
        // Arrange
        var tenantId = 1;
        var wrongTenantId = 2;
        var claims = new List<Claim>
        {
            new("TenantId", tenantId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _httpContext.User = new ClaimsPrincipal(identity);
        _httpContext.Request.QueryString = new QueryString($"?tenantId={wrongTenantId}");

        var routeData = new RouteData();
        routeData.Values["tenantId"] = tenantId; // Should use this

        var actionContext = new ActionContext(
            _httpContext,
            routeData,
            new ActionDescriptor());

        var authContext = new AuthorizationFilterContext(
            actionContext,
            new List<IFilterMetadata>());

        var attribute = new TenantAuthorizationAttribute();

        // Act
        await attribute.OnAuthorizationAsync(authContext);

        // Assert
        Assert.Null(authContext.Result); // Should succeed because route data matches
    }
}
