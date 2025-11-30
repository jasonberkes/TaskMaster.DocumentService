using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Tests.Helpers;
using TaskMaster.DocumentService.Data.Repositories;

namespace TaskMaster.DocumentService.Core.Tests.Repositories;

/// <summary>
/// Unit tests for TenantRepository.
/// </summary>
public class TenantRepositoryTests : IDisposable
{
    private readonly TenantRepository _repository;
    private readonly Data.DocumentServiceDbContext _context;

    public TenantRepositoryTests()
    {
        _context = TestDbContextFactory.Create();
        _repository = new TenantRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsTenant()
    {
        // Arrange
        var tenant = new Tenant
        {
            TenantType = "Organization",
            Name = "Test Org",
            Slug = "test-org",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Tenants.AddAsync(tenant);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(tenant.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tenant.Id, result.Id);
        Assert.Equal("Test Org", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange & Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetBySlugAsync_WithValidSlug_ReturnsTenant()
    {
        // Arrange
        var tenant = new Tenant
        {
            TenantType = "Organization",
            Name = "Test Org",
            Slug = "test-org",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Tenants.AddAsync(tenant);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetBySlugAsync("test-org");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-org", result.Slug);
    }

    [Fact]
    public async Task GetBySlugAsync_WithInvalidSlug_ReturnsNull()
    {
        // Arrange & Act
        var result = await _repository.GetBySlugAsync("non-existent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetActiveTenantsAsync_ReturnsOnlyActiveTenants()
    {
        // Arrange
        var activeTenant = new Tenant
        {
            TenantType = "Organization",
            Name = "Active Tenant",
            Slug = "active-tenant",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var inactiveTenant = new Tenant
        {
            TenantType = "Organization",
            Name = "Inactive Tenant",
            Slug = "inactive-tenant",
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Tenants.AddRangeAsync(activeTenant, inactiveTenant);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveTenantsAsync();

        // Assert
        Assert.Single(result);
        Assert.Contains(result, t => t.Slug == "active-tenant");
        Assert.DoesNotContain(result, t => t.Slug == "inactive-tenant");
    }

    [Fact]
    public async Task GetChildTenantsAsync_ReturnsChildrenOfParent()
    {
        // Arrange
        var parent = new Tenant
        {
            TenantType = "Organization",
            Name = "Parent",
            Slug = "parent",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Tenants.AddAsync(parent);
        await _context.SaveChangesAsync();

        var child1 = new Tenant
        {
            ParentTenantId = parent.Id,
            TenantType = "Team",
            Name = "Child 1",
            Slug = "child-1",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var child2 = new Tenant
        {
            ParentTenantId = parent.Id,
            TenantType = "Team",
            Name = "Child 2",
            Slug = "child-2",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Tenants.AddRangeAsync(child1, child2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetChildTenantsAsync(parent.Id);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, t => t.Slug == "child-1");
        Assert.Contains(result, t => t.Slug == "child-2");
    }

    [Fact]
    public async Task GetByTypeAsync_ReturnsTenantsByType()
    {
        // Arrange
        var org = new Tenant
        {
            TenantType = "Organization",
            Name = "Org",
            Slug = "org",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var team = new Tenant
        {
            TenantType = "Team",
            Name = "Team",
            Slug = "team",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Tenants.AddRangeAsync(org, team);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByTypeAsync("Organization");

        // Assert
        Assert.Single(result);
        Assert.Contains(result, t => t.Slug == "org");
    }

    [Fact]
    public async Task AddAsync_AddsTenantToDatabase()
    {
        // Arrange
        var tenant = new Tenant
        {
            TenantType = "Organization",
            Name = "New Tenant",
            Slug = "new-tenant",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await _repository.AddAsync(tenant);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _repository.GetBySlugAsync("new-tenant");
        Assert.NotNull(result);
        Assert.Equal("New Tenant", result.Name);
    }

    [Fact]
    public async Task Update_UpdatesTenantInDatabase()
    {
        // Arrange
        var tenant = new Tenant
        {
            TenantType = "Organization",
            Name = "Original Name",
            Slug = "original-slug",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Tenants.AddAsync(tenant);
        await _context.SaveChangesAsync();

        // Act
        tenant.Name = "Updated Name";
        tenant.UpdatedAt = DateTime.UtcNow;
        _repository.Update(tenant);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _repository.GetByIdAsync(tenant.Id);
        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.Name);
        Assert.NotNull(result.UpdatedAt);
    }

    [Fact]
    public async Task Remove_DeletesTenantFromDatabase()
    {
        // Arrange
        var tenant = new Tenant
        {
            TenantType = "Organization",
            Name = "To Delete",
            Slug = "to-delete",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Tenants.AddAsync(tenant);
        await _context.SaveChangesAsync();
        var tenantId = tenant.Id;

        // Act
        _repository.Remove(tenant);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _repository.GetByIdAsync(tenantId);
        Assert.Null(result);
    }

    [Fact]
    public async Task CountAsync_ReturnsCorrectCount()
    {
        // Arrange
        var tenants = new[]
        {
            new Tenant { TenantType = "Organization", Name = "Tenant 1", Slug = "tenant-1", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Tenant { TenantType = "Organization", Name = "Tenant 2", Slug = "tenant-2", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Tenant { TenantType = "Team", Name = "Tenant 3", Slug = "tenant-3", IsActive = true, CreatedAt = DateTime.UtcNow }
        };
        await _context.Tenants.AddRangeAsync(tenants);
        await _context.SaveChangesAsync();

        // Act
        var totalCount = await _repository.CountAsync();
        var orgCount = await _repository.CountAsync(t => t.TenantType == "Organization");

        // Assert
        Assert.Equal(3, totalCount);
        Assert.Equal(2, orgCount);
    }

    [Fact]
    public async Task AnyAsync_ReturnsCorrectResult()
    {
        // Arrange
        var tenant = new Tenant
        {
            TenantType = "Organization",
            Name = "Test",
            Slug = "test",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Tenants.AddAsync(tenant);
        await _context.SaveChangesAsync();

        // Act
        var exists = await _repository.AnyAsync(t => t.Slug == "test");
        var notExists = await _repository.AnyAsync(t => t.Slug == "non-existent");

        // Assert
        Assert.True(exists);
        Assert.False(notExists);
    }
}
