using System.Security.Claims;
using TaskMaster.DocumentService.Api.Extensions;
using Xunit;

namespace TaskMaster.DocumentService.Api.Tests.Extensions;

/// <summary>
/// Unit tests for <see cref="ClaimsPrincipalExtensions"/>.
/// </summary>
public class ClaimsPrincipalExtensionsTests
{
    /// <summary>
    /// Test that GetTenantId returns the correct tenant ID.
    /// </summary>
    [Fact]
    public void GetTenantId_WithValidTenantIdClaim_ReturnsCorrectId()
    {
        // Arrange
        var tenantId = 42;
        var claims = new List<Claim>
        {
            new("TenantId", tenantId.ToString())
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.GetTenantId();

        // Assert
        Assert.Equal(tenantId, result);
    }

    /// <summary>
    /// Test that GetTenantId returns null when claim is missing.
    /// </summary>
    [Fact]
    public void GetTenantId_WithoutTenantIdClaim_ReturnsNull()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "TestUser")
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.GetTenantId();

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Test that GetTenantId returns null when claim value is not a valid integer.
    /// </summary>
    [Fact]
    public void GetTenantId_WithInvalidTenantIdClaim_ReturnsNull()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("TenantId", "not-a-number")
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.GetTenantId();

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Test that GetTenantName returns the correct tenant name.
    /// </summary>
    [Fact]
    public void GetTenantName_WithValidTenantNameClaim_ReturnsCorrectName()
    {
        // Arrange
        var tenantName = "Test Tenant";
        var claims = new List<Claim>
        {
            new("TenantName", tenantName)
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.GetTenantName();

        // Assert
        Assert.Equal(tenantName, result);
    }

    /// <summary>
    /// Test that GetTenantName returns null when claim is missing.
    /// </summary>
    [Fact]
    public void GetTenantName_WithoutTenantNameClaim_ReturnsNull()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "TestUser")
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.GetTenantName();

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Test that GetAuthenticationType returns the correct authentication type.
    /// </summary>
    [Theory]
    [InlineData("ApiKey")]
    [InlineData("Bearer")]
    [InlineData("Basic")]
    public void GetAuthenticationType_WithValidClaim_ReturnsCorrectType(string authType)
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("AuthenticationType", authType)
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.GetAuthenticationType();

        // Assert
        Assert.Equal(authType, result);
    }

    /// <summary>
    /// Test that GetAuthenticationType returns null when claim is missing.
    /// </summary>
    [Fact]
    public void GetAuthenticationType_WithoutClaim_ReturnsNull()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "TestUser")
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.GetAuthenticationType();

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Test that HasAccessToTenant returns true when tenant IDs match.
    /// </summary>
    [Fact]
    public void HasAccessToTenant_WithMatchingTenantId_ReturnsTrue()
    {
        // Arrange
        var tenantId = 1;
        var claims = new List<Claim>
        {
            new("TenantId", tenantId.ToString())
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.HasAccessToTenant(tenantId);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Test that HasAccessToTenant returns false when tenant IDs don't match.
    /// </summary>
    [Fact]
    public void HasAccessToTenant_WithNonMatchingTenantId_ReturnsFalse()
    {
        // Arrange
        var userTenantId = 1;
        var requestedTenantId = 2;
        var claims = new List<Claim>
        {
            new("TenantId", userTenantId.ToString())
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.HasAccessToTenant(requestedTenantId);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Test that HasAccessToTenant returns false when TenantId claim is missing.
    /// </summary>
    [Fact]
    public void HasAccessToTenant_WithoutTenantIdClaim_ReturnsFalse()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "TestUser")
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.HasAccessToTenant(1);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Test that methods work with multiple claims.
    /// </summary>
    [Fact]
    public void Extensions_WithMultipleClaims_WorkCorrectly()
    {
        // Arrange
        var tenantId = 42;
        var tenantName = "Multi-Tenant Corp";
        var authType = "Bearer";
        var claims = new List<Claim>
        {
            new("TenantId", tenantId.ToString()),
            new("TenantName", tenantName),
            new("AuthenticationType", authType),
            new(ClaimTypes.Name, "TestUser"),
            new(ClaimTypes.Email, "test@example.com")
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        // Act & Assert
        Assert.Equal(tenantId, principal.GetTenantId());
        Assert.Equal(tenantName, principal.GetTenantName());
        Assert.Equal(authType, principal.GetAuthenticationType());
        Assert.True(principal.HasAccessToTenant(tenantId));
        Assert.False(principal.HasAccessToTenant(99));
    }
}
