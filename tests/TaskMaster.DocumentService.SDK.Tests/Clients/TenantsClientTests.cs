using System.Net;
using System.Text;
using System.Text.Json;
using Moq;
using Moq.Protected;
using TaskMaster.DocumentService.SDK.Clients;
using TaskMaster.DocumentService.SDK.DTOs;

namespace TaskMaster.DocumentService.SDK.Tests.Clients;

/// <summary>
/// Unit tests for TenantsClient.
/// </summary>
public class TenantsClientTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly TenantsClient _tenantsClient;

    public TenantsClientTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://api.example.com/")
        };
        _tenantsClient = new TenantsClient(_httpClient);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsTenant_WhenExists()
    {
        // Arrange
        var tenantId = 1;
        var expectedTenant = new TenantDto
        {
            Id = tenantId,
            Name = "Test Tenant",
            Slug = "test-tenant",
            TenantType = "Organization",
            IsActive = true
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedTenant);

        // Act
        var result = await _tenantsClient.GetByIdAsync(tenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedTenant.Id, result.Id);
        Assert.Equal(expectedTenant.Name, result.Name);
    }

    [Fact]
    public async Task GetBySlugAsync_ReturnsTenant_WhenExists()
    {
        // Arrange
        var slug = "test-tenant";
        var expectedTenant = new TenantDto
        {
            Id = 1,
            Name = "Test Tenant",
            Slug = slug,
            TenantType = "Organization",
            IsActive = true
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedTenant);

        // Act
        var result = await _tenantsClient.GetBySlugAsync(slug);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedTenant.Slug, result.Slug);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsTenants_WhenExist()
    {
        // Arrange
        var expectedTenants = new List<TenantDto>
        {
            new() { Id = 1, Name = "Tenant 1", Slug = "tenant-1", TenantType = "Organization", IsActive = true },
            new() { Id = 2, Name = "Tenant 2", Slug = "tenant-2", TenantType = "Organization", IsActive = true }
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedTenants);

        // Act
        var result = await _tenantsClient.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyActiveTenants_WhenActiveOnlyIsTrue()
    {
        // Arrange
        var expectedTenants = new List<TenantDto>
        {
            new() { Id = 1, Name = "Tenant 1", Slug = "tenant-1", TenantType = "Organization", IsActive = true },
            new() { Id = 2, Name = "Tenant 2", Slug = "tenant-2", TenantType = "Organization", IsActive = true }
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedTenants);

        // Act
        var result = await _tenantsClient.GetAllAsync(activeOnly: true);

        // Assert
        Assert.NotNull(result);
        Assert.All(result, t => Assert.True(t.IsActive));
    }

    [Fact]
    public async Task GetChildTenantsAsync_ReturnsChildTenants_WhenExist()
    {
        // Arrange
        var parentTenantId = 1;
        var expectedChildTenants = new List<TenantDto>
        {
            new() { Id = 2, Name = "Child 1", ParentTenantId = parentTenantId, TenantType = "Team", IsActive = true },
            new() { Id = 3, Name = "Child 2", ParentTenantId = parentTenantId, TenantType = "Team", IsActive = true }
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedChildTenants);

        // Act
        var result = await _tenantsClient.GetChildTenantsAsync(parentTenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, t => Assert.Equal(parentTenantId, t.ParentTenantId));
    }

    [Fact]
    public async Task CreateAsync_ReturnsCreatedTenant_WhenRequestIsValid()
    {
        // Arrange
        var tenant = new TenantDto
        {
            Name = "New Tenant",
            Slug = "new-tenant",
            TenantType = "Organization",
            IsActive = true
        };

        var expectedTenant = new TenantDto
        {
            Id = 1,
            Name = tenant.Name,
            Slug = tenant.Slug,
            TenantType = tenant.TenantType,
            IsActive = tenant.IsActive
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedTenant);

        // Act
        var result = await _tenantsClient.CreateAsync(tenant);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedTenant.Name, result.Name);
        Assert.Equal(expectedTenant.Slug, result.Slug);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsUpdatedTenant_WhenRequestIsValid()
    {
        // Arrange
        var tenantId = 1;
        var tenant = new TenantDto
        {
            Id = tenantId,
            Name = "Updated Tenant",
            Slug = "updated-tenant",
            TenantType = "Organization",
            IsActive = true
        };

        SetupHttpResponse(HttpStatusCode.OK, tenant);

        // Act
        var result = await _tenantsClient.UpdateAsync(tenantId, tenant);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tenant.Name, result.Name);
        Assert.Equal(tenant.Slug, result.Slug);
    }

    [Fact]
    public async Task DeleteAsync_CompletesSuccessfully_WhenTenantExists()
    {
        // Arrange
        var tenantId = 1;
        SetupHttpResponse(HttpStatusCode.OK, new { });

        // Act
        await _tenantsClient.DeleteAsync(tenantId);

        // Assert
        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    private void SetupHttpResponse<T>(HttpStatusCode statusCode, T content)
    {
        var json = JsonSerializer.Serialize(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var response = new HttpResponseMessage
        {
            StatusCode = statusCode,
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }
}
