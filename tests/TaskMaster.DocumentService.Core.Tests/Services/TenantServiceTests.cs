using Microsoft.Extensions.Logging;
using Moq;
using TaskMaster.DocumentService.Core.DTOs;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Repositories;
using TaskMaster.DocumentService.Core.Services;

namespace TaskMaster.DocumentService.Core.Tests.Services;

/// <summary>
/// Unit tests for TenantService.
/// Tests follow AAA (Arrange-Act-Assert) pattern.
/// </summary>
public class TenantServiceTests
{
    private readonly Mock<ITenantRepository> _mockRepository;
    private readonly Mock<ILogger<TenantService>> _mockLogger;
    private readonly TenantService _service;

    public TenantServiceTests()
    {
        _mockRepository = new Mock<ITenantRepository>();
        _mockLogger = new Mock<ILogger<TenantService>>();
        _service = new TenantService(_mockRepository.Object, _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TenantService(null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TenantService(_mockRepository.Object, null!));
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsTenantDto()
    {
        // Arrange
        var tenantId = 1;
        var tenant = CreateSampleTenant(tenantId);
        _mockRepository.Setup(r => r.GetByIdAsync(tenantId, default))
            .ReturnsAsync(tenant);

        // Act
        var result = await _service.GetByIdAsync(tenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tenantId, result.Id);
        Assert.Equal(tenant.Name, result.Name);
        Assert.Equal(tenant.Slug, result.Slug);
        _mockRepository.Verify(r => r.GetByIdAsync(tenantId, default), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var tenantId = 999;
        _mockRepository.Setup(r => r.GetByIdAsync(tenantId, default))
            .ReturnsAsync((Tenant?)null);

        // Act
        var result = await _service.GetByIdAsync(tenantId);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByIdAsync(tenantId, default), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenRepositoryThrows_PropagatesException()
    {
        // Arrange
        var tenantId = 1;
        _mockRepository.Setup(r => r.GetByIdAsync(tenantId, default))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.GetByIdAsync(tenantId));
    }

    #endregion

    #region GetBySlugAsync Tests

    [Fact]
    public async Task GetBySlugAsync_WithValidSlug_ReturnsTenantDto()
    {
        // Arrange
        var slug = "test-tenant";
        var tenant = CreateSampleTenant(1, slug);
        _mockRepository.Setup(r => r.GetBySlugAsync(slug, default))
            .ReturnsAsync(tenant);

        // Act
        var result = await _service.GetBySlugAsync(slug);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(slug, result.Slug);
        Assert.Equal(tenant.Name, result.Name);
        _mockRepository.Verify(r => r.GetBySlugAsync(slug, default), Times.Once);
    }

    [Fact]
    public async Task GetBySlugAsync_WithNullSlug_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetBySlugAsync(null!));
    }

    [Fact]
    public async Task GetBySlugAsync_WithEmptySlug_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetBySlugAsync(""));
    }

    [Fact]
    public async Task GetBySlugAsync_WithWhitespaceSlug_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetBySlugAsync("   "));
    }

    [Fact]
    public async Task GetBySlugAsync_WithNonExistentSlug_ReturnsNull()
    {
        // Arrange
        var slug = "non-existent";
        _mockRepository.Setup(r => r.GetBySlugAsync(slug, default))
            .ReturnsAsync((Tenant?)null);

        // Act
        var result = await _service.GetBySlugAsync(slug);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetBySlugAsync(slug, default), Times.Once);
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllTenants()
    {
        // Arrange
        var tenants = new List<Tenant>
        {
            CreateSampleTenant(1, "tenant-1"),
            CreateSampleTenant(2, "tenant-2"),
            CreateSampleTenant(3, "tenant-3")
        };
        _mockRepository.Setup(r => r.GetAllAsync(true, default))
            .ReturnsAsync(tenants);

        // Act
        var result = await _service.GetAllAsync(true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
        _mockRepository.Verify(r => r.GetAllAsync(true, default), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithActiveOnlyTrue_CallsRepositoryWithActiveOnly()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetAllAsync(true, default))
            .ReturnsAsync(new List<Tenant>());

        // Act
        await _service.GetAllAsync(true);

        // Assert
        _mockRepository.Verify(r => r.GetAllAsync(true, default), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithActiveOnlyFalse_CallsRepositoryWithoutFilter()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetAllAsync(false, default))
            .ReturnsAsync(new List<Tenant>());

        // Act
        await _service.GetAllAsync(false);

        // Assert
        _mockRepository.Verify(r => r.GetAllAsync(false, default), Times.Once);
    }

    #endregion

    #region GetChildTenantsAsync Tests

    [Fact]
    public async Task GetChildTenantsAsync_ReturnsChildTenants()
    {
        // Arrange
        var parentId = 1;
        var childTenants = new List<Tenant>
        {
            CreateSampleTenant(2, "child-1", parentId),
            CreateSampleTenant(3, "child-2", parentId)
        };
        _mockRepository.Setup(r => r.GetChildTenantsAsync(parentId, true, default))
            .ReturnsAsync(childTenants);

        // Act
        var result = await _service.GetChildTenantsAsync(parentId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, dto => Assert.Equal(parentId, dto.ParentTenantId));
        _mockRepository.Verify(r => r.GetChildTenantsAsync(parentId, true, default), Times.Once);
    }

    #endregion

    #region GetRootTenantsAsync Tests

    [Fact]
    public async Task GetRootTenantsAsync_ReturnsRootTenants()
    {
        // Arrange
        var rootTenants = new List<Tenant>
        {
            CreateSampleTenant(1, "root-1"),
            CreateSampleTenant(2, "root-2")
        };
        _mockRepository.Setup(r => r.GetRootTenantsAsync(true, default))
            .ReturnsAsync(rootTenants);

        // Act
        var result = await _service.GetRootTenantsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, dto => Assert.Null(dto.ParentTenantId));
        _mockRepository.Verify(r => r.GetRootTenantsAsync(true, default), Times.Once);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidDto_CreatesAndReturnsTenantDto()
    {
        // Arrange
        var createDto = new CreateTenantDto
        {
            TenantType = "Organization",
            Name = "Test Tenant",
            Slug = "test-tenant",
            Settings = "{\"key\":\"value\"}",
            RetentionPolicies = "{\"days\":90}"
        };

        _mockRepository.Setup(r => r.SlugExistsAsync(createDto.Slug, null, default))
            .ReturnsAsync(false);

        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<Tenant>(), default))
            .ReturnsAsync((Tenant t, CancellationToken ct) =>
            {
                t.Id = 1;
                t.CreatedAt = DateTime.UtcNow;
                return t;
            });

        // Act
        var result = await _service.CreateAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(createDto.Name, result.Name);
        Assert.Equal(createDto.Slug, result.Slug);
        Assert.Equal(createDto.TenantType, result.TenantType);
        Assert.True(result.IsActive);
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<Tenant>(), default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithNullDto_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.CreateAsync(null!));
    }

    [Fact]
    public async Task CreateAsync_WithEmptyTenantType_ThrowsArgumentException()
    {
        // Arrange
        var createDto = new CreateTenantDto
        {
            TenantType = "",
            Name = "Test Tenant",
            Slug = "test-tenant"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(createDto));
        Assert.Contains("TenantType", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var createDto = new CreateTenantDto
        {
            TenantType = "Organization",
            Name = "",
            Slug = "test-tenant"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(createDto));
        Assert.Contains("Name", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_WithEmptySlug_ThrowsArgumentException()
    {
        // Arrange
        var createDto = new CreateTenantDto
        {
            TenantType = "Organization",
            Name = "Test Tenant",
            Slug = ""
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(createDto));
        Assert.Contains("Slug", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidSlugFormat_ThrowsArgumentException()
    {
        // Arrange
        var createDto = new CreateTenantDto
        {
            TenantType = "Organization",
            Name = "Test Tenant",
            Slug = "Test Tenant!" // Invalid characters
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(createDto));
        Assert.Contains("Slug", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_WithExistingSlug_ThrowsInvalidOperationException()
    {
        // Arrange
        var createDto = new CreateTenantDto
        {
            TenantType = "Organization",
            Name = "Test Tenant",
            Slug = "existing-slug"
        };

        _mockRepository.Setup(r => r.SlugExistsAsync(createDto.Slug, null, default))
            .ReturnsAsync(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(createDto));
        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_WithNonExistentParentId_ThrowsInvalidOperationException()
    {
        // Arrange
        var createDto = new CreateTenantDto
        {
            ParentTenantId = 999,
            TenantType = "Department",
            Name = "Test Department",
            Slug = "test-dept"
        };

        _mockRepository.Setup(r => r.SlugExistsAsync(createDto.Slug, null, default))
            .ReturnsAsync(false);

        _mockRepository.Setup(r => r.GetByIdAsync(999, default))
            .ReturnsAsync((Tenant?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(createDto));
        Assert.Contains("Parent tenant", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_WithInactiveParent_ThrowsInvalidOperationException()
    {
        // Arrange
        var parentTenant = CreateSampleTenant(1, "parent");
        parentTenant.IsActive = false;

        var createDto = new CreateTenantDto
        {
            ParentTenantId = 1,
            TenantType = "Department",
            Name = "Test Department",
            Slug = "test-dept"
        };

        _mockRepository.Setup(r => r.SlugExistsAsync(createDto.Slug, null, default))
            .ReturnsAsync(false);

        _mockRepository.Setup(r => r.GetByIdAsync(1, default))
            .ReturnsAsync(parentTenant);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(createDto));
        Assert.Contains("inactive", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_WithValidParent_CreatesChildTenant()
    {
        // Arrange
        var parentTenant = CreateSampleTenant(1, "parent");

        var createDto = new CreateTenantDto
        {
            ParentTenantId = 1,
            TenantType = "Department",
            Name = "Test Department",
            Slug = "test-dept"
        };

        _mockRepository.Setup(r => r.SlugExistsAsync(createDto.Slug, null, default))
            .ReturnsAsync(false);

        _mockRepository.Setup(r => r.GetByIdAsync(1, default))
            .ReturnsAsync(parentTenant);

        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<Tenant>(), default))
            .ReturnsAsync((Tenant t, CancellationToken ct) =>
            {
                t.Id = 2;
                t.CreatedAt = DateTime.UtcNow;
                return t;
            });

        // Act
        var result = await _service.CreateAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.ParentTenantId);
        Assert.Equal(createDto.Name, result.Name);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidDto_UpdatesAndReturnsTenantDto()
    {
        // Arrange
        var tenantId = 1;
        var existingTenant = CreateSampleTenant(tenantId);
        var updateDto = new UpdateTenantDto
        {
            Name = "Updated Name",
            Settings = "{\"updated\":\"value\"}",
            IsActive = false
        };

        _mockRepository.Setup(r => r.GetByIdAsync(tenantId, default))
            .ReturnsAsync(existingTenant);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Tenant>(), default))
            .ReturnsAsync((Tenant t, CancellationToken ct) => t);

        // Act
        var result = await _service.UpdateAsync(tenantId, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(updateDto.Name, result.Name);
        Assert.Equal(updateDto.Settings, result.Settings);
        Assert.Equal(updateDto.IsActive, result.IsActive);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Tenant>(), default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNullDto_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.UpdateAsync(1, null!));
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var tenantId = 999;
        var updateDto = new UpdateTenantDto { Name = "Updated Name" };

        _mockRepository.Setup(r => r.GetByIdAsync(tenantId, default))
            .ReturnsAsync((Tenant?)null);

        // Act
        var result = await _service.UpdateAsync(tenantId, updateDto);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Tenant>(), default), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithPartialDto_UpdatesOnlyProvidedFields()
    {
        // Arrange
        var tenantId = 1;
        var existingTenant = CreateSampleTenant(tenantId);
        var originalName = existingTenant.Name;
        var updateDto = new UpdateTenantDto
        {
            Settings = "{\"updated\":\"value\"}"
            // Name and IsActive not provided
        };

        _mockRepository.Setup(r => r.GetByIdAsync(tenantId, default))
            .ReturnsAsync(existingTenant);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Tenant>(), default))
            .ReturnsAsync((Tenant t, CancellationToken ct) => t);

        // Act
        var result = await _service.UpdateAsync(tenantId, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(originalName, result.Name); // Name unchanged
        Assert.Equal(updateDto.Settings, result.Settings); // Settings updated
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_DeletesAndReturnsTrue()
    {
        // Arrange
        var tenantId = 1;
        _mockRepository.Setup(r => r.DeleteAsync(tenantId, default))
            .ReturnsAsync(true);

        // Act
        var result = await _service.DeleteAsync(tenantId);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.DeleteAsync(tenantId, default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentId_ReturnsFalse()
    {
        // Arrange
        var tenantId = 999;
        _mockRepository.Setup(r => r.DeleteAsync(tenantId, default))
            .ReturnsAsync(false);

        // Act
        var result = await _service.DeleteAsync(tenantId);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.DeleteAsync(tenantId, default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenRepositoryThrows_PropagatesException()
    {
        // Arrange
        var tenantId = 1;
        _mockRepository.Setup(r => r.DeleteAsync(tenantId, default))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.DeleteAsync(tenantId));
    }

    #endregion

    #region Helper Methods

    private static Tenant CreateSampleTenant(int id, string? slug = null, int? parentTenantId = null)
    {
        return new Tenant
        {
            Id = id,
            ParentTenantId = parentTenantId,
            TenantType = "Organization",
            Name = $"Test Tenant {id}",
            Slug = slug ?? $"test-tenant-{id}",
            Settings = "{\"key\":\"value\"}",
            RetentionPolicies = "{\"days\":90}",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    #endregion
}
