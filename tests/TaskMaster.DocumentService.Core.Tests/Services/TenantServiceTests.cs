using Microsoft.Extensions.Logging;
using Moq;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Interfaces;
using TaskMaster.DocumentService.Core.Services;

namespace TaskMaster.DocumentService.Core.Tests.Services;

/// <summary>
/// Unit tests for TenantService.
/// </summary>
public class TenantServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ITenantRepository> _mockTenantRepository;
    private readonly Mock<ILogger<TenantService>> _mockLogger;
    private readonly TenantService _tenantService;

    public TenantServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockTenantRepository = new Mock<ITenantRepository>();
        _mockLogger = new Mock<ILogger<TenantService>>();

        // Setup UnitOfWork to return the mocked repository
        _mockUnitOfWork.Setup(x => x.Tenants).Returns(_mockTenantRepository.Object);

        _tenantService = new TenantService(_mockUnitOfWork.Object, _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange & Act
        var service = new TenantService(_mockUnitOfWork.Object, _mockLogger.Object);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithNullUnitOfWork_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new TenantService(null!, _mockLogger.Object));

        Assert.Equal("unitOfWork", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new TenantService(_mockUnitOfWork.Object, null!));

        Assert.Equal("logger", exception.ParamName);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnTenant()
    {
        // Arrange
        var tenantId = 1;
        var expectedTenant = new Tenant
        {
            Id = tenantId,
            Name = "Test Tenant",
            Slug = "test-tenant",
            TenantType = "Organization",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _mockTenantRepository
            .Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTenant);

        // Act
        var result = await _tenantService.GetByIdAsync(tenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedTenant.Id, result.Id);
        Assert.Equal(expectedTenant.Name, result.Name);
        Assert.Equal(expectedTenant.Slug, result.Slug);
        _mockTenantRepository.Verify(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ShouldReturnNull()
    {
        // Arrange
        var tenantId = 999;
        _mockTenantRepository
            .Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        // Act
        var result = await _tenantService.GetByIdAsync(tenantId);

        // Assert
        Assert.Null(result);
        _mockTenantRepository.Verify(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetByIdAsync_WithInvalidId_ShouldThrowArgumentException(int invalidId)
    {
        // Arrange & Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _tenantService.GetByIdAsync(invalidId));

        Assert.Equal("id", exception.ParamName);
    }

    #endregion

    #region GetBySlugAsync Tests

    [Fact]
    public async Task GetBySlugAsync_WithValidSlug_ShouldReturnTenant()
    {
        // Arrange
        var slug = "test-tenant";
        var expectedTenant = new Tenant
        {
            Id = 1,
            Name = "Test Tenant",
            Slug = slug,
            TenantType = "Organization",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _mockTenantRepository
            .Setup(x => x.GetBySlugAsync(slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTenant);

        // Act
        var result = await _tenantService.GetBySlugAsync(slug);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedTenant.Slug, result.Slug);
        _mockTenantRepository.Verify(x => x.GetBySlugAsync(slug, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetBySlugAsync_WithNonExistentSlug_ShouldReturnNull()
    {
        // Arrange
        var slug = "non-existent";
        _mockTenantRepository
            .Setup(x => x.GetBySlugAsync(slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        // Act
        var result = await _tenantService.GetBySlugAsync(slug);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetBySlugAsync_WithInvalidSlug_ShouldThrowArgumentException(string invalidSlug)
    {
        // Arrange & Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _tenantService.GetBySlugAsync(invalidSlug));

        Assert.Equal("slug", exception.ParamName);
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllTenants()
    {
        // Arrange
        var tenants = new List<Tenant>
        {
            new Tenant { Id = 1, Name = "Tenant 1", Slug = "tenant-1", TenantType = "Organization", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Tenant { Id = 2, Name = "Tenant 2", Slug = "tenant-2", TenantType = "Team", IsActive = true, CreatedAt = DateTime.UtcNow }
        };

        _mockTenantRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenants);

        // Act
        var result = await _tenantService.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        _mockTenantRepository.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithNoTenants_ShouldReturnEmptyCollection()
    {
        // Arrange
        _mockTenantRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Tenant>());

        // Act
        var result = await _tenantService.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region GetActiveTenantsAsync Tests

    [Fact]
    public async Task GetActiveTenantsAsync_ShouldReturnOnlyActiveTenants()
    {
        // Arrange
        var activeTenants = new List<Tenant>
        {
            new Tenant { Id = 1, Name = "Active Tenant 1", Slug = "active-1", TenantType = "Organization", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Tenant { Id = 2, Name = "Active Tenant 2", Slug = "active-2", TenantType = "Team", IsActive = true, CreatedAt = DateTime.UtcNow }
        };

        _mockTenantRepository
            .Setup(x => x.GetActiveTenantsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeTenants);

        // Act
        var result = await _tenantService.GetActiveTenantsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, tenant => Assert.True(tenant.IsActive));
    }

    #endregion

    #region GetChildTenantsAsync Tests

    [Fact]
    public async Task GetChildTenantsAsync_WithValidParentId_ShouldReturnChildTenants()
    {
        // Arrange
        var parentId = 1;
        var childTenants = new List<Tenant>
        {
            new Tenant { Id = 2, Name = "Child 1", Slug = "child-1", TenantType = "Team", ParentTenantId = parentId, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Tenant { Id = 3, Name = "Child 2", Slug = "child-2", TenantType = "Team", ParentTenantId = parentId, IsActive = true, CreatedAt = DateTime.UtcNow }
        };

        _mockTenantRepository
            .Setup(x => x.GetChildTenantsAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(childTenants);

        // Act
        var result = await _tenantService.GetChildTenantsAsync(parentId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, tenant => Assert.Equal(parentId, tenant.ParentTenantId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetChildTenantsAsync_WithInvalidParentId_ShouldThrowArgumentException(int invalidParentId)
    {
        // Arrange & Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _tenantService.GetChildTenantsAsync(invalidParentId));

        Assert.Equal("parentTenantId", exception.ParamName);
    }

    #endregion

    #region GetByTypeAsync Tests

    [Fact]
    public async Task GetByTypeAsync_WithValidType_ShouldReturnTenantsOfType()
    {
        // Arrange
        var tenantType = "Organization";
        var tenants = new List<Tenant>
        {
            new Tenant { Id = 1, Name = "Org 1", Slug = "org-1", TenantType = tenantType, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Tenant { Id = 2, Name = "Org 2", Slug = "org-2", TenantType = tenantType, IsActive = true, CreatedAt = DateTime.UtcNow }
        };

        _mockTenantRepository
            .Setup(x => x.GetByTypeAsync(tenantType, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenants);

        // Act
        var result = await _tenantService.GetByTypeAsync(tenantType);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, tenant => Assert.Equal(tenantType, tenant.TenantType));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetByTypeAsync_WithInvalidType_ShouldThrowArgumentException(string invalidType)
    {
        // Arrange & Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _tenantService.GetByTypeAsync(invalidType));

        Assert.Equal("tenantType", exception.ParamName);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidTenant_ShouldCreateAndReturnTenant()
    {
        // Arrange
        var newTenant = new Tenant
        {
            Name = "New Tenant",
            Slug = "new-tenant",
            TenantType = "Organization",
            IsActive = true
        };

        _mockTenantRepository
            .Setup(x => x.GetBySlugAsync(newTenant.Slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        _mockTenantRepository
            .Setup(x => x.AddAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant t, CancellationToken ct) =>
            {
                t.Id = 1;
                return t;
            });

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _tenantService.CreateAsync(newTenant);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(newTenant.Name, result.Name);
        Assert.NotEqual(default(DateTime), result.CreatedAt);
        _mockTenantRepository.Verify(x => x.AddAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithExistingSlug_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var existingTenant = new Tenant
        {
            Id = 1,
            Name = "Existing Tenant",
            Slug = "existing-slug",
            TenantType = "Organization",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var newTenant = new Tenant
        {
            Name = "New Tenant",
            Slug = "existing-slug",
            TenantType = "Organization",
            IsActive = true
        };

        _mockTenantRepository
            .Setup(x => x.GetBySlugAsync(newTenant.Slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTenant);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _tenantService.CreateAsync(newTenant));

        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_WithValidParentTenant_ShouldCreateTenant()
    {
        // Arrange
        var parentTenant = new Tenant
        {
            Id = 1,
            Name = "Parent Tenant",
            Slug = "parent",
            TenantType = "Organization",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var newTenant = new Tenant
        {
            Name = "Child Tenant",
            Slug = "child",
            TenantType = "Team",
            ParentTenantId = 1,
            IsActive = true
        };

        _mockTenantRepository
            .Setup(x => x.GetBySlugAsync(newTenant.Slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        _mockTenantRepository
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentTenant);

        _mockTenantRepository
            .Setup(x => x.AddAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant t, CancellationToken ct) =>
            {
                t.Id = 2;
                return t;
            });

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _tenantService.CreateAsync(newTenant);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.ParentTenantId);
    }

    [Fact]
    public async Task CreateAsync_WithNonExistentParent_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var newTenant = new Tenant
        {
            Name = "Child Tenant",
            Slug = "child",
            TenantType = "Team",
            ParentTenantId = 999,
            IsActive = true
        };

        _mockTenantRepository
            .Setup(x => x.GetBySlugAsync(newTenant.Slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        _mockTenantRepository
            .Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _tenantService.CreateAsync(newTenant));

        Assert.Contains("does not exist", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_WithInactiveParent_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var inactiveParent = new Tenant
        {
            Id = 1,
            Name = "Inactive Parent",
            Slug = "inactive-parent",
            TenantType = "Organization",
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };

        var newTenant = new Tenant
        {
            Name = "Child Tenant",
            Slug = "child",
            TenantType = "Team",
            ParentTenantId = 1,
            IsActive = true
        };

        _mockTenantRepository
            .Setup(x => x.GetBySlugAsync(newTenant.Slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        _mockTenantRepository
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inactiveParent);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _tenantService.CreateAsync(newTenant));

        Assert.Contains("not active", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_WithNullTenant_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _tenantService.CreateAsync(null!));

        Assert.Equal("tenant", exception.ParamName);
    }

    [Theory]
    [InlineData(null, "slug", "Organization")]
    [InlineData("", "slug", "Organization")]
    [InlineData("   ", "slug", "Organization")]
    [InlineData("Name", null, "Organization")]
    [InlineData("Name", "", "Organization")]
    [InlineData("Name", "   ", "Organization")]
    [InlineData("Name", "slug", null)]
    [InlineData("Name", "slug", "")]
    [InlineData("Name", "slug", "   ")]
    public async Task CreateAsync_WithInvalidTenantProperties_ShouldThrowArgumentException(
        string name, string slug, string tenantType)
    {
        // Arrange
        var tenant = new Tenant
        {
            Name = name,
            Slug = slug,
            TenantType = tenantType,
            IsActive = true
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _tenantService.CreateAsync(tenant));
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidTenant_ShouldUpdateAndReturnTenant()
    {
        // Arrange
        var existingTenant = new Tenant
        {
            Id = 1,
            Name = "Original Name",
            Slug = "original-slug",
            TenantType = "Organization",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var updatedTenant = new Tenant
        {
            Id = 1,
            Name = "Updated Name",
            Slug = "original-slug",
            TenantType = "Organization",
            IsActive = true,
            CreatedAt = existingTenant.CreatedAt
        };

        _mockTenantRepository
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTenant);

        _mockTenantRepository
            .Setup(x => x.GetBySlugAsync(updatedTenant.Slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTenant);

        _mockTenantRepository
            .Setup(x => x.Update(It.IsAny<Tenant>()));

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _tenantService.UpdateAsync(updatedTenant);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.Name);
        Assert.NotNull(result.UpdatedAt);
        _mockTenantRepository.Verify(x => x.Update(It.IsAny<Tenant>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentTenant_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = 999,
            Name = "Non-existent",
            Slug = "non-existent",
            TenantType = "Organization",
            IsActive = true
        };

        _mockTenantRepository
            .Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _tenantService.UpdateAsync(tenant));

        Assert.Contains("does not exist", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_WithConflictingSlug_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var existingTenant1 = new Tenant
        {
            Id = 1,
            Name = "Tenant 1",
            Slug = "tenant-1",
            TenantType = "Organization",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var existingTenant2 = new Tenant
        {
            Id = 2,
            Name = "Tenant 2",
            Slug = "tenant-2",
            TenantType = "Organization",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var updatedTenant = new Tenant
        {
            Id = 1,
            Name = "Tenant 1",
            Slug = "tenant-2", // Trying to use tenant 2's slug
            TenantType = "Organization",
            IsActive = true
        };

        _mockTenantRepository
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTenant1);

        _mockTenantRepository
            .Setup(x => x.GetBySlugAsync("tenant-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTenant2);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _tenantService.UpdateAsync(updatedTenant));

        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_WithCircularReference_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var tenant1 = new Tenant
        {
            Id = 1,
            Name = "Tenant 1",
            Slug = "tenant-1",
            TenantType = "Organization",
            ParentTenantId = 2,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var tenant2 = new Tenant
        {
            Id = 2,
            Name = "Tenant 2",
            Slug = "tenant-2",
            TenantType = "Organization",
            ParentTenantId = null,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Try to make tenant 2's parent = tenant 1 (which would create a circular reference)
        var updatedTenant2 = new Tenant
        {
            Id = 2,
            Name = "Tenant 2",
            Slug = "tenant-2",
            TenantType = "Organization",
            ParentTenantId = 1,
            IsActive = true
        };

        _mockTenantRepository
            .Setup(x => x.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant2);

        _mockTenantRepository
            .Setup(x => x.GetBySlugAsync("tenant-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant2);

        _mockTenantRepository
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant1);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _tenantService.UpdateAsync(updatedTenant2));

        Assert.Contains("circular reference", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_WithSelfAsParent_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var existingTenant = new Tenant
        {
            Id = 1,
            Name = "Tenant",
            Slug = "tenant",
            TenantType = "Organization",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var updatedTenant = new Tenant
        {
            Id = 1,
            Name = "Tenant",
            Slug = "tenant",
            TenantType = "Organization",
            ParentTenantId = 1, // Self as parent
            IsActive = true
        };

        _mockTenantRepository
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTenant);

        _mockTenantRepository
            .Setup(x => x.GetBySlugAsync("tenant", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTenant);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _tenantService.UpdateAsync(updatedTenant));

        Assert.Contains("cannot be its own parent", exception.Message);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_ShouldDeleteAndReturnTrue()
    {
        // Arrange
        var tenantId = 1;
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Tenant to Delete",
            Slug = "tenant-to-delete",
            TenantType = "Organization",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _mockTenantRepository
            .Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        _mockTenantRepository
            .Setup(x => x.GetChildTenantsAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Tenant>());

        _mockTenantRepository
            .Setup(x => x.Remove(It.IsAny<Tenant>()));

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _tenantService.DeleteAsync(tenantId);

        // Assert
        Assert.True(result);
        _mockTenantRepository.Verify(x => x.Remove(It.IsAny<Tenant>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentId_ShouldReturnFalse()
    {
        // Arrange
        var tenantId = 999;
        _mockTenantRepository
            .Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        // Act
        var result = await _tenantService.DeleteAsync(tenantId);

        // Assert
        Assert.False(result);
        _mockTenantRepository.Verify(x => x.Remove(It.IsAny<Tenant>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithChildTenants_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var tenantId = 1;
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Parent Tenant",
            Slug = "parent-tenant",
            TenantType = "Organization",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var childTenants = new List<Tenant>
        {
            new Tenant { Id = 2, Name = "Child", Slug = "child", TenantType = "Team", ParentTenantId = tenantId, IsActive = true, CreatedAt = DateTime.UtcNow }
        };

        _mockTenantRepository
            .Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        _mockTenantRepository
            .Setup(x => x.GetChildTenantsAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(childTenants);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _tenantService.DeleteAsync(tenantId));

        Assert.Contains("has child tenants", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task DeleteAsync_WithInvalidId_ShouldThrowArgumentException(int invalidId)
    {
        // Arrange & Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _tenantService.DeleteAsync(invalidId));

        Assert.Equal("id", exception.ParamName);
    }

    #endregion

    #region ExistsBySlugAsync Tests

    [Fact]
    public async Task ExistsBySlugAsync_WithExistingSlug_ShouldReturnTrue()
    {
        // Arrange
        var slug = "existing-slug";
        var tenant = new Tenant
        {
            Id = 1,
            Name = "Existing Tenant",
            Slug = slug,
            TenantType = "Organization",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _mockTenantRepository
            .Setup(x => x.GetBySlugAsync(slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        // Act
        var result = await _tenantService.ExistsBySlugAsync(slug);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsBySlugAsync_WithNonExistentSlug_ShouldReturnFalse()
    {
        // Arrange
        var slug = "non-existent";
        _mockTenantRepository
            .Setup(x => x.GetBySlugAsync(slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        // Act
        var result = await _tenantService.ExistsBySlugAsync(slug);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region ValidateHierarchyAsync Tests

    [Fact]
    public async Task ValidateHierarchyAsync_WithNoParent_ShouldReturnTrue()
    {
        // Arrange
        var tenantId = 1;

        // Act
        var result = await _tenantService.ValidateHierarchyAsync(tenantId, null);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateHierarchyAsync_WithValidParent_ShouldReturnTrue()
    {
        // Arrange
        var tenantId = 2;
        var parentId = 1;
        var parent = new Tenant
        {
            Id = parentId,
            Name = "Parent",
            Slug = "parent",
            TenantType = "Organization",
            ParentTenantId = null,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _mockTenantRepository
            .Setup(x => x.GetByIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parent);

        // Act
        var result = await _tenantService.ValidateHierarchyAsync(tenantId, parentId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateHierarchyAsync_WithSelfAsParent_ShouldReturnFalse()
    {
        // Arrange
        var tenantId = 1;

        // Act
        var result = await _tenantService.ValidateHierarchyAsync(tenantId, tenantId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateHierarchyAsync_WithCircularReference_ShouldReturnFalse()
    {
        // Arrange
        var tenant1Id = 1;
        var tenant2Id = 2;

        var tenant1 = new Tenant
        {
            Id = tenant1Id,
            Name = "Tenant 1",
            Slug = "tenant-1",
            TenantType = "Organization",
            ParentTenantId = tenant2Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // When validating if tenant2 can have tenant1 as parent,
        // the algorithm will traverse up: tenant2 -> tenant1 (proposed parent)
        // Then it checks tenant1's parent chain: tenant1 has parent = tenant2
        // This creates a circular reference: tenant2 -> tenant1 -> tenant2
        _mockTenantRepository
            .Setup(x => x.GetByIdAsync(tenant1Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant1);

        // Act (trying to set tenant 1 as parent of tenant 2, which would create circular reference)
        var result = await _tenantService.ValidateHierarchyAsync(tenant2Id, tenant1Id);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ValidateHierarchyAsync_WithInvalidTenantId_ShouldThrowArgumentException(int invalidId)
    {
        // Arrange & Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _tenantService.ValidateHierarchyAsync(invalidId, 1));

        Assert.Equal("tenantId", exception.ParamName);
    }

    #endregion
}
