using Microsoft.EntityFrameworkCore;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Data.Context;
using TaskMaster.DocumentService.Data.Repositories;

namespace TaskMaster.DocumentService.Core.Tests.Repositories;

/// <summary>
/// Unit tests for the TemplateRepository class using in-memory database.
/// </summary>
public class TemplateRepositoryTests : IDisposable
{
    private readonly DocumentServiceDbContext _context;
    private readonly TemplateRepository _repository;

    public TemplateRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<DocumentServiceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DocumentServiceDbContext(options);
        _repository = new TemplateRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TemplateRepository(null!));
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsTemplate()
    {
        // Arrange
        var template = new DocumentTemplate
        {
            TenantId = 100,
            Name = "Test Template",
            Content = "Content"
        };
        await _context.DocumentTemplates.AddAsync(template);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(template.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(template.Id, result.Id);
        Assert.Equal("Test Template", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WithDeletedTemplate_ReturnsNull()
    {
        // Arrange
        var template = new DocumentTemplate
        {
            TenantId = 100,
            Name = "Deleted Template",
            Content = "Content",
            IsDeleted = true
        };
        await _context.DocumentTemplates.AddAsync(template);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(template.Id);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetByTenantIdAsync Tests

    [Fact]
    public async Task GetByTenantIdAsync_ReturnsOnlyActiveTenantTemplates()
    {
        // Arrange
        await _context.DocumentTemplates.AddRangeAsync(
            new DocumentTemplate { TenantId = 100, Name = "Template 1", Content = "C1", IsActive = true, IsDeleted = false, IsCurrentVersion = true },
            new DocumentTemplate { TenantId = 100, Name = "Template 2", Content = "C2", IsActive = false, IsDeleted = false, IsCurrentVersion = true },
            new DocumentTemplate { TenantId = 100, Name = "Template 3", Content = "C3", IsActive = true, IsDeleted = true, IsCurrentVersion = true },
            new DocumentTemplate { TenantId = 200, Name = "Template 4", Content = "C4", IsActive = true, IsDeleted = false, IsCurrentVersion = true }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByTenantIdAsync(100, false);

        // Assert
        Assert.Single(result);
        Assert.Equal("Template 1", result[0].Name);
    }

    [Fact]
    public async Task GetByTenantIdAsync_WithIncludePublic_ReturnsPublicTemplates()
    {
        // Arrange
        await _context.DocumentTemplates.AddRangeAsync(
            new DocumentTemplate { TenantId = 100, Name = "Private", Content = "C1", IsPublic = false, IsActive = true, IsDeleted = false, IsCurrentVersion = true },
            new DocumentTemplate { TenantId = 200, Name = "Public", Content = "C2", IsPublic = true, IsActive = true, IsDeleted = false, IsCurrentVersion = true }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByTenantIdAsync(100, true);

        // Assert
        Assert.Equal(2, result.Count);
    }

    #endregion

    #region GetByTypeAsync Tests

    [Fact]
    public async Task GetByTypeAsync_FiltersByTemplateType()
    {
        // Arrange
        await _context.DocumentTemplates.AddRangeAsync(
            new DocumentTemplate { TenantId = 100, Name = "Email 1", Content = "C1", TemplateType = "Email", IsActive = true, IsDeleted = false, IsCurrentVersion = true },
            new DocumentTemplate { TenantId = 100, Name = "Document 1", Content = "C2", TemplateType = "Document", IsActive = true, IsDeleted = false, IsCurrentVersion = true },
            new DocumentTemplate { TenantId = 100, Name = "Email 2", Content = "C3", TemplateType = "Email", IsActive = true, IsDeleted = false, IsCurrentVersion = true }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByTypeAsync(100, "Email", false);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, t => Assert.Equal("Email", t.TemplateType));
    }

    #endregion

    #region SearchByNameAsync Tests

    [Fact]
    public async Task SearchByNameAsync_FindsMatchingTemplates()
    {
        // Arrange
        await _context.DocumentTemplates.AddRangeAsync(
            new DocumentTemplate { TenantId = 100, Name = "Invoice Template", Content = "C1", IsActive = true, IsDeleted = false, IsCurrentVersion = true },
            new DocumentTemplate { TenantId = 100, Name = "Receipt Template", Content = "C2", IsActive = true, IsDeleted = false, IsCurrentVersion = true },
            new DocumentTemplate { TenantId = 100, Name = "Invoice Summary", Content = "C3", IsActive = true, IsDeleted = false, IsCurrentVersion = true }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.SearchByNameAsync(100, "Invoice");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, t => Assert.Contains("Invoice", t.Name));
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_AddsTemplateToDatabase()
    {
        // Arrange
        var template = new DocumentTemplate
        {
            TenantId = 100,
            Name = "New Template",
            Content = "Content"
        };

        // Act
        var result = await _repository.CreateAsync(template);

        // Assert
        Assert.True(result.Id > 0);
        Assert.Equal("New Template", result.Name);

        var dbTemplate = await _context.DocumentTemplates.FindAsync(result.Id);
        Assert.NotNull(dbTemplate);
        Assert.Equal("New Template", dbTemplate.Name);
    }

    [Fact]
    public async Task CreateAsync_SetsDefaultValues()
    {
        // Arrange
        var template = new DocumentTemplate
        {
            TenantId = 100,
            Name = "Template",
            Content = "Content"
        };

        // Act
        var result = await _repository.CreateAsync(template);

        // Assert
        Assert.False(result.IsDeleted);
        Assert.True(result.IsActive);
        Assert.True(result.CreatedAt <= DateTime.UtcNow);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_UpdatesTemplateInDatabase()
    {
        // Arrange
        var template = new DocumentTemplate
        {
            TenantId = 100,
            Name = "Original Name",
            Content = "Content"
        };
        await _context.DocumentTemplates.AddAsync(template);
        await _context.SaveChangesAsync();
        _context.Entry(template).State = EntityState.Detached;

        template.Name = "Updated Name";

        // Act
        var result = await _repository.UpdateAsync(template);

        // Assert
        Assert.Equal("Updated Name", result.Name);
        Assert.NotNull(result.UpdatedAt);

        var dbTemplate = await _context.DocumentTemplates.FindAsync(template.Id);
        Assert.Equal("Updated Name", dbTemplate!.Name);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_SoftDeletesTemplate()
    {
        // Arrange
        var template = new DocumentTemplate
        {
            TenantId = 100,
            Name = "Template to Delete",
            Content = "Content"
        };
        await _context.DocumentTemplates.AddAsync(template);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.DeleteAsync(template.Id, "testuser");

        // Assert
        Assert.True(result);

        var dbTemplate = await _context.DocumentTemplates.FindAsync(template.Id);
        Assert.True(dbTemplate!.IsDeleted);
        Assert.NotNull(dbTemplate.DeletedAt);
        Assert.Equal("testuser", dbTemplate.DeletedBy);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentId_ReturnsFalse()
    {
        // Act
        var result = await _repository.DeleteAsync(999, "testuser");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_WithAlreadyDeleted_ReturnsFalse()
    {
        // Arrange
        var template = new DocumentTemplate
        {
            TenantId = 100,
            Name = "Template",
            Content = "Content",
            IsDeleted = true
        };
        await _context.DocumentTemplates.AddAsync(template);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.DeleteAsync(template.Id, "testuser");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region LogUsageAsync Tests

    [Fact]
    public async Task LogUsageAsync_AddsLogToDatabase()
    {
        // Arrange
        var template = new DocumentTemplate
        {
            TenantId = 100,
            Name = "Template",
            Content = "Content"
        };
        await _context.DocumentTemplates.AddAsync(template);
        await _context.SaveChangesAsync();

        var usageLog = new TemplateUsageLog
        {
            TemplateId = template.Id,
            TenantId = 100,
            UsedBy = "testuser",
            Status = "Success"
        };

        // Act
        var result = await _repository.LogUsageAsync(usageLog);

        // Assert
        Assert.True(result.Id > 0);
        Assert.Equal("testuser", result.UsedBy);

        var dbLog = await _context.TemplateUsageLog.FindAsync(result.Id);
        Assert.NotNull(dbLog);
        Assert.Equal("Success", dbLog.Status);
    }

    #endregion

    #region GetUsageStatisticsAsync Tests

    [Fact]
    public async Task GetUsageStatisticsAsync_ReturnsUsageForTemplate()
    {
        // Arrange
        var template = new DocumentTemplate
        {
            TenantId = 100,
            Name = "Template",
            Content = "Content"
        };
        await _context.DocumentTemplates.AddAsync(template);
        await _context.SaveChangesAsync();

        await _context.TemplateUsageLog.AddRangeAsync(
            new TemplateUsageLog { TemplateId = template.Id, TenantId = 100, UsedAt = DateTime.UtcNow.AddDays(-5) },
            new TemplateUsageLog { TemplateId = template.Id, TenantId = 100, UsedAt = DateTime.UtcNow.AddDays(-3) },
            new TemplateUsageLog { TemplateId = template.Id, TenantId = 100, UsedAt = DateTime.UtcNow.AddDays(-1) }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetUsageStatisticsAsync(template.Id);

        // Assert
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetUsageStatisticsAsync_WithDateRange_FiltersCorrectly()
    {
        // Arrange
        var template = new DocumentTemplate
        {
            TenantId = 100,
            Name = "Template",
            Content = "Content"
        };
        await _context.DocumentTemplates.AddAsync(template);
        await _context.SaveChangesAsync();

        var baseDate = DateTime.UtcNow;
        await _context.TemplateUsageLog.AddRangeAsync(
            new TemplateUsageLog { TemplateId = template.Id, TenantId = 100, UsedAt = baseDate.AddDays(-10) },
            new TemplateUsageLog { TemplateId = template.Id, TenantId = 100, UsedAt = baseDate.AddDays(-5) },
            new TemplateUsageLog { TemplateId = template.Id, TenantId = 100, UsedAt = baseDate.AddDays(-1) }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetUsageStatisticsAsync(
            template.Id,
            startDate: baseDate.AddDays(-7),
            endDate: baseDate);

        // Assert
        Assert.Equal(2, result.Count);
    }

    #endregion
}
