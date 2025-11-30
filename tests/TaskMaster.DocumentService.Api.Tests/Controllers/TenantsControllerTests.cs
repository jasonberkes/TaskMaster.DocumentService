using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TaskMaster.DocumentService.Api.Controllers;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Interfaces;
using Xunit;

namespace TaskMaster.DocumentService.Api.Tests.Controllers;

/// <summary>
/// Unit tests for <see cref="TenantsController"/>.
/// </summary>
public class TenantsControllerTests
{
    private readonly Mock<ITenantRepository> _tenantRepository;
    private readonly Mock<ILogger<TenantsController>> _logger;
    private readonly TenantsController _controller;

    public TenantsControllerTests()
    {
        _tenantRepository = new Mock<ITenantRepository>();
        _logger = new Mock<ILogger<TenantsController>>();
        _controller = new TenantsController(_tenantRepository.Object, _logger.Object);

        // Setup default authenticated user
        var claims = new List<Claim>
        {
            new("TenantId", "1"),
            new("TenantName", "Test Tenant"),
            new(ClaimTypes.Name, "testuser"),
            new("AuthenticationType", "Bearer")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    /// <summary>
    /// Test that GetTenant returns tenant information when tenant exists.
    /// </summary>
    [Fact]
    public async Task GetTenant_WithValidTenantId_ReturnsOkResult()
    {
        // Arrange
        var tenantId = 1;
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Test Tenant",
            Slug = "test-tenant",
            TenantType = "Organization",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _tenantRepository
            .Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        // Act
        var result = await _controller.GetTenant(tenantId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    /// <summary>
    /// Test that GetTenant returns NotFound when tenant does not exist.
    /// </summary>
    [Fact]
    public async Task GetTenant_WithNonExistentTenant_ReturnsNotFound()
    {
        // Arrange
        var tenantId = 999;
        _tenantRepository
            .Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        // Act
        var result = await _controller.GetTenant(tenantId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// Test that GetCurrentTenant returns current user's tenant information.
    /// </summary>
    [Fact]
    public void GetCurrentTenant_WithAuthenticatedUser_ReturnsOkResult()
    {
        // Act
        var result = _controller.GetCurrentTenant();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        // Verify the response contains expected properties
        var value = okResult.Value;
        Assert.NotNull(value);
        var properties = value.GetType().GetProperties();
        Assert.Contains(properties, p => p.Name == "tenantId");
        Assert.Contains(properties, p => p.Name == "tenantName");
        Assert.Contains(properties, p => p.Name == "authenticationType");
        Assert.Contains(properties, p => p.Name == "isAuthenticated");
    }

    /// <summary>
    /// Test that GetChildTenants returns child tenants.
    /// </summary>
    [Fact]
    public async Task GetChildTenants_WithValidTenantId_ReturnsOkResult()
    {
        // Arrange
        var tenantId = 1;
        var childTenants = new List<Tenant>
        {
            new Tenant
            {
                Id = 2,
                Name = "Child Tenant 1",
                Slug = "child-tenant-1",
                TenantType = "Team",
                IsActive = true,
                ParentTenantId = tenantId
            },
            new Tenant
            {
                Id = 3,
                Name = "Child Tenant 2",
                Slug = "child-tenant-2",
                TenantType = "Team",
                IsActive = true,
                ParentTenantId = tenantId
            }
        };

        _tenantRepository
            .Setup(x => x.GetChildTenantsAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(childTenants);

        // Act
        var result = await _controller.GetChildTenants(tenantId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    /// <summary>
    /// Test that GetChildTenants returns empty list when no child tenants exist.
    /// </summary>
    [Fact]
    public async Task GetChildTenants_WithNoChildTenants_ReturnsEmptyList()
    {
        // Arrange
        var tenantId = 1;
        _tenantRepository
            .Setup(x => x.GetChildTenantsAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Tenant>());

        // Act
        var result = await _controller.GetChildTenants(tenantId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    /// <summary>
    /// Test that controller properly extracts tenant information from claims.
    /// </summary>
    [Fact]
    public void GetCurrentTenant_ExtractsCorrectClaimValues()
    {
        // Arrange
        var expectedTenantId = 42;
        var expectedTenantName = "Custom Tenant";
        var expectedAuthType = "ApiKey";

        var claims = new List<Claim>
        {
            new("TenantId", expectedTenantId.ToString()),
            new("TenantName", expectedTenantName),
            new(ClaimTypes.Name, "customuser"),
            new("AuthenticationType", expectedAuthType)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var controller = new TenantsController(_tenantRepository.Object, _logger.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            }
        };

        // Act
        var result = controller.GetCurrentTenant();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value;
        Assert.NotNull(value);

        var tenantIdProp = value.GetType().GetProperty("tenantId");
        var tenantNameProp = value.GetType().GetProperty("tenantName");
        var authTypeProp = value.GetType().GetProperty("authenticationType");

        Assert.NotNull(tenantIdProp);
        Assert.NotNull(tenantNameProp);
        Assert.NotNull(authTypeProp);

        Assert.Equal(expectedTenantId, tenantIdProp.GetValue(value));
        Assert.Equal(expectedTenantName, tenantNameProp.GetValue(value));
        Assert.Equal(expectedAuthType, authTypeProp.GetValue(value));
    }

    /// <summary>
    /// Test that repository is called with correct parameters.
    /// </summary>
    [Fact]
    public async Task GetTenant_CallsRepositoryWithCorrectId()
    {
        // Arrange
        var tenantId = 123;
        _tenantRepository
            .Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        // Act
        await _controller.GetTenant(tenantId);

        // Assert
        _tenantRepository.Verify(
            x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Test that GetChildTenants calls repository with correct parameters.
    /// </summary>
    [Fact]
    public async Task GetChildTenants_CallsRepositoryWithCorrectId()
    {
        // Arrange
        var tenantId = 123;
        _tenantRepository
            .Setup(x => x.GetChildTenantsAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Tenant>());

        // Act
        await _controller.GetChildTenants(tenantId);

        // Assert
        _tenantRepository.Verify(
            x => x.GetChildTenantsAsync(tenantId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
