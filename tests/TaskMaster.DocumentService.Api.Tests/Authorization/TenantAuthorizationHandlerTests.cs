using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Moq;
using TaskMaster.DocumentService.Api.Authorization;
using Xunit;

namespace TaskMaster.DocumentService.Api.Tests.Authorization;

/// <summary>
/// Unit tests for <see cref="TenantAuthorizationHandler"/>.
/// </summary>
public class TenantAuthorizationHandlerTests
{
    private readonly Mock<ILogger<TenantAuthorizationHandler>> _logger;
    private readonly TenantAuthorizationHandler _handler;

    public TenantAuthorizationHandlerTests()
    {
        _logger = new Mock<ILogger<TenantAuthorizationHandler>>();
        _handler = new TenantAuthorizationHandler(_logger.Object);
    }

    /// <summary>
    /// Test that authorization succeeds when user's tenant ID matches the required tenant ID.
    /// </summary>
    [Fact]
    public async Task HandleRequirementAsync_WithMatchingTenantId_Succeeds()
    {
        // Arrange
        var tenantId = 1;
        var claims = new List<Claim>
        {
            new("TenantId", tenantId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        var requirement = new TenantAuthorizationRequirement(tenantId);
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    /// <summary>
    /// Test that authorization fails when user's tenant ID does not match the required tenant ID.
    /// </summary>
    [Fact]
    public async Task HandleRequirementAsync_WithNonMatchingTenantId_Fails()
    {
        // Arrange
        var userTenantId = 1;
        var requiredTenantId = 2;
        var claims = new List<Claim>
        {
            new("TenantId", userTenantId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        var requirement = new TenantAuthorizationRequirement(requiredTenantId);
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    /// <summary>
    /// Test that authorization fails when user does not have a TenantId claim.
    /// </summary>
    [Fact]
    public async Task HandleRequirementAsync_WithoutTenantIdClaim_Fails()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "TestUser")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        var requirement = new TenantAuthorizationRequirement(1);
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    /// <summary>
    /// Test that authorization fails when TenantId claim is not a valid integer.
    /// </summary>
    [Fact]
    public async Task HandleRequirementAsync_WithInvalidTenantIdClaim_Fails()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("TenantId", "not-a-number")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        var requirement = new TenantAuthorizationRequirement(1);
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    /// <summary>
    /// Test that authorization succeeds with different tenant IDs.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(999)]
    public async Task HandleRequirementAsync_WithVariousTenantIds_Succeeds(int tenantId)
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("TenantId", tenantId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        var requirement = new TenantAuthorizationRequirement(tenantId);
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    /// <summary>
    /// Test that authorization fails when TenantId claim is empty.
    /// </summary>
    [Fact]
    public async Task HandleRequirementAsync_WithEmptyTenantIdClaim_Fails()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("TenantId", "")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        var requirement = new TenantAuthorizationRequirement(1);
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }
}
