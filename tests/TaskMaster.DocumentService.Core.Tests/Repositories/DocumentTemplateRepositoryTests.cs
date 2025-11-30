using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Tests.Helpers;
using TaskMaster.DocumentService.Data.Repositories;

namespace TaskMaster.DocumentService.Core.Tests.Repositories;

/// <summary>
/// Unit tests for DocumentTemplateRepository.
/// </summary>
public class DocumentTemplateRepositoryTests : IDisposable
{
    private readonly DocumentTemplateRepository _repository;
    private readonly Data.DocumentServiceDbContext _context;

    public DocumentTemplateRepositoryTests()
    {
        _context = TestDbContextFactory.Create();
        _repository = new DocumentTemplateRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private async Task<(Tenant tenant, DocumentType docType)> SeedTestDataAsync()
    {
        var tenant = new Tenant
        {
            TenantType = "Organization",
            Name = "Test Org",
            Slug = "test-org",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var docType = new DocumentType
        {
            Name = "Invoice",
            DisplayName = "Invoice",
            IsContentIndexed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Tenants.AddAsync(tenant);
        await _context.DocumentTypes.AddAsync(docType);
        await _context.SaveChangesAsync();

        return (tenant, docType);
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsTemplate()
    {
        // Arrange
        var (tenant, docType) = await SeedTestDataAsync();
        var template = new DocumentTemplate
        {
            TenantId = tenant.Id,
            DocumentTypeId = docType.Id,
            Name = "Test Template",
            BlobPath = "/templates/test.html",
            MimeType = "text/html",
            IsCurrentVersion = true,
            IsActive = true,
            Version = 1,
            CreatedAt = DateTime.UtcNow
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
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByTenantIdAsync_ReturnsAllTemplatesForTenant()
    {
        // Arrange
        var (tenant, docType) = await SeedTestDataAsync();
        var template1 = new DocumentTemplate
        {
            TenantId = tenant.Id,
            DocumentTypeId = docType.Id,
            Name = "Template 1",
            BlobPath = "/templates/1.html",
            MimeType = "text/html",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var template2 = new DocumentTemplate
        {
            TenantId = tenant.Id,
            DocumentTypeId = docType.Id,
            Name = "Template 2",
            BlobPath = "/templates/2.html",
            MimeType = "text/html",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.DocumentTemplates.AddRangeAsync(template1, template2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByTenantIdAsync(tenant.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetByTenantIdAsync_ExcludesDeletedByDefault()
    {
        // Arrange
        var (tenant, docType) = await SeedTestDataAsync();
        var template1 = new DocumentTemplate
        {
            TenantId = tenant.Id,
            DocumentTypeId = docType.Id,
            Name = "Active Template",
            BlobPath = "/templates/active.html",
            MimeType = "text/html",
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };
        var template2 = new DocumentTemplate
        {
            TenantId = tenant.Id,
            DocumentTypeId = docType.Id,
            Name = "Deleted Template",
            BlobPath = "/templates/deleted.html",
            MimeType = "text/html",
            IsActive = true,
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        await _context.DocumentTemplates.AddRangeAsync(template1, template2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByTenantIdAsync(tenant.Id, includeDeleted: false);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Active Template", result.First().Name);
    }

    [Fact]
    public async Task GetByTenantIdAsync_ExcludesInactiveByDefault()
    {
        // Arrange
        var (tenant, docType) = await SeedTestDataAsync();
        var template1 = new DocumentTemplate
        {
            TenantId = tenant.Id,
            DocumentTypeId = docType.Id,
            Name = "Active Template",
            BlobPath = "/templates/active.html",
            MimeType = "text/html",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var template2 = new DocumentTemplate
        {
            TenantId = tenant.Id,
            DocumentTypeId = docType.Id,
            Name = "Inactive Template",
            BlobPath = "/templates/inactive.html",
            MimeType = "text/html",
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };
        await _context.DocumentTemplates.AddRangeAsync(template1, template2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByTenantIdAsync(tenant.Id, includeInactive: false);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Active Template", result.First().Name);
    }

    [Fact]
    public async Task GetByDocumentTypeIdAsync_ReturnsTemplatesForDocumentType()
    {
        // Arrange
        var (tenant, docType) = await SeedTestDataAsync();
        var template = new DocumentTemplate
        {
            TenantId = tenant.Id,
            DocumentTypeId = docType.Id,
            Name = "Invoice Template",
            BlobPath = "/templates/invoice.html",
            MimeType = "text/html",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.DocumentTemplates.AddAsync(template);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByDocumentTypeIdAsync(docType.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Invoice Template", result.First().Name);
    }

    [Fact]
    public async Task GetByCategoryAsync_ReturnsTemplatesInCategory()
    {
        // Arrange
        var (tenant, docType) = await SeedTestDataAsync();
        var template1 = new DocumentTemplate
        {
            TenantId = tenant.Id,
            DocumentTypeId = docType.Id,
            Name = "Invoice Template 1",
            Category = "Invoices",
            BlobPath = "/templates/invoice1.html",
            MimeType = "text/html",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var template2 = new DocumentTemplate
        {
            TenantId = tenant.Id,
            DocumentTypeId = docType.Id,
            Name = "Report Template",
            Category = "Reports",
            BlobPath = "/templates/report.html",
            MimeType = "text/html",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.DocumentTemplates.AddRangeAsync(template1, template2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByCategoryAsync(tenant.Id, "Invoices");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Invoice Template 1", result.First().Name);
    }

    [Fact]
    public async Task GetActiveTemplatesAsync_ReturnsOnlyActiveTemplates()
    {
        // Arrange
        var (tenant, docType) = await SeedTestDataAsync();
        var template1 = new DocumentTemplate
        {
            TenantId = tenant.Id,
            DocumentTypeId = docType.Id,
            Name = "Active Template",
            BlobPath = "/templates/active.html",
            MimeType = "text/html",
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };
        var template2 = new DocumentTemplate
        {
            TenantId = tenant.Id,
            DocumentTypeId = docType.Id,
            Name = "Inactive Template",
            BlobPath = "/templates/inactive.html",
            MimeType = "text/html",
            IsActive = false,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };
        await _context.DocumentTemplates.AddRangeAsync(template1, template2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveTemplatesAsync(tenant.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.True(result.First().IsActive);
    }

    [Fact]
    public async Task SoftDeleteAsync_MarksTemplateAsDeleted()
    {
        // Arrange
        var (tenant, docType) = await SeedTestDataAsync();
        var template = new DocumentTemplate
        {
            TenantId = tenant.Id,
            DocumentTypeId = docType.Id,
            Name = "Template to Delete",
            BlobPath = "/templates/delete.html",
            MimeType = "text/html",
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };
        await _context.DocumentTemplates.AddAsync(template);
        await _context.SaveChangesAsync();

        // Act
        await _repository.SoftDeleteAsync(template.Id, "testuser");
        await _context.SaveChangesAsync();

        // Assert
        var result = await _repository.GetByIdAsync(template.Id);
        Assert.NotNull(result);
        Assert.True(result.IsDeleted);
        Assert.False(result.IsActive);
        Assert.NotNull(result.DeletedAt);
        Assert.Equal("testuser", result.DeletedBy);
    }

    [Fact]
    public async Task SoftDeleteAsync_WithNonExistentTemplate_ThrowsInvalidOperationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _repository.SoftDeleteAsync(999, "testuser"));
    }

    [Fact]
    public async Task RestoreAsync_RestoresDeletedTemplate()
    {
        // Arrange
        var (tenant, docType) = await SeedTestDataAsync();
        var template = new DocumentTemplate
        {
            TenantId = tenant.Id,
            DocumentTypeId = docType.Id,
            Name = "Deleted Template",
            BlobPath = "/templates/deleted.html",
            MimeType = "text/html",
            IsActive = false,
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow,
            DeletedBy = "testuser",
            CreatedAt = DateTime.UtcNow
        };
        await _context.DocumentTemplates.AddAsync(template);
        await _context.SaveChangesAsync();

        // Act
        await _repository.RestoreAsync(template.Id);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _repository.GetByIdAsync(template.Id);
        Assert.NotNull(result);
        Assert.False(result.IsDeleted);
        Assert.Null(result.DeletedAt);
        Assert.Null(result.DeletedBy);
    }

    [Fact]
    public async Task RestoreAsync_WithNonExistentTemplate_ThrowsInvalidOperationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _repository.RestoreAsync(999));
    }

    [Fact]
    public async Task SearchByNameAsync_ReturnsMatchingTemplates()
    {
        // Arrange
        var (tenant, docType) = await SeedTestDataAsync();
        var template1 = new DocumentTemplate
        {
            TenantId = tenant.Id,
            DocumentTypeId = docType.Id,
            Name = "Invoice Template",
            BlobPath = "/templates/invoice.html",
            MimeType = "text/html",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var template2 = new DocumentTemplate
        {
            TenantId = tenant.Id,
            DocumentTypeId = docType.Id,
            Name = "Report Template",
            BlobPath = "/templates/report.html",
            MimeType = "text/html",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.DocumentTemplates.AddRangeAsync(template1, template2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.SearchByNameAsync(tenant.Id, "Invoice");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Contains("Invoice", result.First().Name);
    }

    [Fact]
    public async Task GetCurrentVersionAsync_ReturnsCurrentVersion()
    {
        // Arrange
        var (tenant, docType) = await SeedTestDataAsync();
        var parentTemplate = new DocumentTemplate
        {
            TenantId = tenant.Id,
            DocumentTypeId = docType.Id,
            Name = "Template v1",
            BlobPath = "/templates/v1.html",
            MimeType = "text/html",
            Version = 1,
            IsCurrentVersion = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.DocumentTemplates.AddAsync(parentTemplate);
        await _context.SaveChangesAsync();

        var currentVersion = new DocumentTemplate
        {
            TenantId = tenant.Id,
            DocumentTypeId = docType.Id,
            Name = "Template v2",
            BlobPath = "/templates/v2.html",
            MimeType = "text/html",
            Version = 2,
            ParentTemplateId = parentTemplate.Id,
            IsCurrentVersion = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.DocumentTemplates.AddAsync(currentVersion);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetCurrentVersionAsync(parentTemplate.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Version);
        Assert.True(result.IsCurrentVersion);
    }

    [Fact]
    public async Task GetVersionsAsync_ReturnsAllVersionsOrderedByVersionDescending()
    {
        // Arrange
        var (tenant, docType) = await SeedTestDataAsync();
        var parentTemplate = new DocumentTemplate
        {
            TenantId = tenant.Id,
            DocumentTypeId = docType.Id,
            Name = "Template v1",
            BlobPath = "/templates/v1.html",
            MimeType = "text/html",
            Version = 1,
            IsCurrentVersion = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.DocumentTemplates.AddAsync(parentTemplate);
        await _context.SaveChangesAsync();

        var version2 = new DocumentTemplate
        {
            TenantId = tenant.Id,
            DocumentTypeId = docType.Id,
            Name = "Template v2",
            BlobPath = "/templates/v2.html",
            MimeType = "text/html",
            Version = 2,
            ParentTemplateId = parentTemplate.Id,
            IsCurrentVersion = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var version3 = new DocumentTemplate
        {
            TenantId = tenant.Id,
            DocumentTypeId = docType.Id,
            Name = "Template v3",
            BlobPath = "/templates/v3.html",
            MimeType = "text/html",
            Version = 3,
            ParentTemplateId = parentTemplate.Id,
            IsCurrentVersion = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.DocumentTemplates.AddRangeAsync(version2, version3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetVersionsAsync(parentTemplate.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Equal(3, result.First().Version);
        Assert.Equal(2, result.Last().Version);
    }
}
