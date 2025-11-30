using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TaskMaster.DocumentService.Core.Authorization;

namespace TaskMaster.DocumentService.Core.Tests.Authorization;

/// <summary>
/// Unit tests for TenantScopedAuthorizationHandler.
/// </summary>
public class TenantScopedAuthorizationHandlerTests
{
    private readonly TenantScopedAuthorizationHandler _handler;

    public TenantScopedAuthorizationHandlerTests()
    {
        _handler = new TenantScopedAuthorizationHandler();
    }

    [Fact]
    public async Task HandleRequirement_WithValidTenantClaim_Succeeds()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var claims = new[]
        {
            new Claim("TenantId", tenantId.ToString()),
            new Claim(ClaimTypes.Name, "test-user")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);
        var requirement = new TenantScopedRequirement();
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirement_WithoutTenantClaim_Fails()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "test-user")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);
        var requirement = new TenantScopedRequirement();
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirement_WithInvalidTenantClaim_Fails()
    {
        // Arrange
        var claims = new[]
        {
            new Claim("TenantId", "not-a-guid"),
            new Claim(ClaimTypes.Name, "test-user")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);
        var requirement = new TenantScopedRequirement();
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirement_WhenNotRequired_Succeeds()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "test-user")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);
        var requirement = new TenantScopedRequirement { RequireTenantAccess = false };
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public void GetTenantId_WithValidClaim_ReturnsGuid()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var claims = new[]
        {
            new Claim("TenantId", tenantId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        // Act
        var result = user.GetTenantId();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tenantId, result.Value);
    }

    [Fact]
    public void GetTenantId_WithoutClaim_ReturnsNull()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "test-user")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        // Act
        var result = user.GetTenantId();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetUserId_WithNameIdentifierClaim_ReturnsUserId()
    {
        // Arrange
        var userId = "user123";
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        // Act
        var result = user.GetUserId();

        // Assert
        Assert.Equal(userId, result);
    }

    [Fact]
    public void GetUserId_WithNameClaim_ReturnsUserId()
    {
        // Arrange
        var userId = "user456";
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, userId)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        // Act
        var result = user.GetUserId();

        // Assert
        Assert.Equal(userId, result);
    }
}
