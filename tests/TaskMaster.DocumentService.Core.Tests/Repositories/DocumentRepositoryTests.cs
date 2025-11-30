using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Tests.Helpers;
using TaskMaster.DocumentService.Data.Repositories;

namespace TaskMaster.DocumentService.Core.Tests.Repositories;

/// <summary>
/// Unit tests for DocumentRepository.
/// </summary>
public class DocumentRepositoryTests : IDisposable
{
    private readonly DocumentRepository _repository;
    private readonly Data.DocumentServiceDbContext _context;

    public DocumentRepositoryTests()
    {
        _context = TestDbContextFactory.Create();
        _repository = new DocumentRepository(_context);
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
    public async Task GetByIdAsync_WithValidId_ReturnsDocument()
    {
        // Arrange
        var (tenant, docType) = await SeedTestDataAsync();
        var document = new Document
        {
            TenantId = tenant.Id,
            DocumentTypeId = docType.Id,
            Title = "Test Document",
            BlobPath = "/test/path",
            IsCurrentVersion = true,
            Version = 1,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Documents.AddAsync(document);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(document.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(document.Id, result.Id);
        Assert.Equal("Test Document", result.Title);
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
    public async Task GetByTenantIdAsync_ReturnsDocumentsForTenant()
    {
        // Arrange
        var (tenant, docType) = await SeedTestDataAsync();
        var doc1 = new Document
        {
            TenantId = tenant.Id,
            DocumentTypeId = docType.Id,
            Title = "Doc 1",
            BlobPath = "/test/path1",
            IsCurrentVersion = true,
            Version = 1,
            CreatedAt = DateTime.UtcNow
        };
        var doc2 = new Document
        {
            TenantId = tenant.Id,
            DocumentTypeId = docType.Id,
            Title = "Doc 2",
            BlobPath = "/test/path2",
            IsCurrentVersion = true,
            Version = 1,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Documents.AddRangeAsync(doc1, doc2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByTenantIdAsync(tenant.Id);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, d => d.Title == "Doc 1");
        Assert.Contains(result, d => d.Title == "Doc 2");
    }

    [Fact]
    public async Task GetByTenantIdAsync_ExcludesDeletedByDefault()
    {
        // Arrange
        var (tenant, docType) = await SeedTestDataAsync();
        var activeDoc = new Document
        {
            TenantId = tenant.Id,
            DocumentTypeId = docType.Id,
            Title = "Active",
            BlobPath = "/test/active",
            IsDeleted = false,
            IsCurrentVersion = true,
            Version = 1,
            CreatedAt = DateTime.UtcNow
        };
        var deletedDoc = new Document
        {
            TenantId = tenant.Id,
            DocumentTypeId = docType.Id,
            Title = "Deleted",
            BlobPath = "/test/deleted",
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow,
            DeletedBy = "test-user",
            IsCurrentVersion = true,
            Version = 1,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Documents.AddRangeAsync(activeDoc, deletedDoc);
        await _context.SaveChangesAsync();

        // Act
        var resultExcluded = await _repository.GetByTenantIdAsync(tenant.Id, includeDeleted: false);
        var resultIncluded = await _repository.GetByTenantIdAsync(tenant.Id, includeDeleted: true);

        // Assert
        Assert.Single(resultExcluded);
        Assert.Contains(resultExcluded, d => d.Title == "Active");
        Assert.Equal(2, resultIncluded.Count());
    }

    [Fact]
    public async Task GetByDocumentTypeIdAsync_ReturnsDocumentsOfType()
    {
        // Arrange
        var (tenant, docType) = await SeedTestDataAsync();
        var doc = new Document
        {
            TenantId = tenant.Id,
            DocumentTypeId = docType.Id,
            Title = "Test Doc",
            BlobPath = "/test/path",
            IsCurrentVersion = true,
            Version = 1,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Documents.AddAsync(doc);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByDocumentTypeIdAsync(docType.Id);

        // Assert
        Assert.Single(result);
        Assert.Contains(result, d => d.Title == "Test Doc");
    }

    [Fact]
    public async Task GetCurrentVersionAsync_ReturnsCurrentVersion()
    {
        // Arrange
        var (tenant, docType) = await SeedTestDataAsync();
        var parentDoc = new Document
        {
            TenantId = tenant.Id,
            DocumentTypeId = docType.Id,
            Title = "Original",
            BlobPath = "/test/original",
            IsCurrentVersion = false,
            Version = 1,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Documents.AddAsync(parentDoc);
        await _context.SaveChangesAsync();

        var currentVersion = new Document
        {
            TenantId = tenant.Id,
            DocumentTypeId = docType.Id,
            ParentDocumentId = parentDoc.Id,
            Title = "Updated",
            BlobPath = "/test/updated",
            IsCurrentVersion = true,
            Version = 2,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Documents.AddAsync(currentVersion);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetCurrentVersionAsync(parentDoc.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Version);
        Assert.True(result.IsCurrentVersion);
    }

    [Fact]
    public async Task GetVersionsAsync_ReturnsAllVersionsOrderedByVersionDesc()
    {
        // Arrange
        var (tenant, docType) = await SeedTestDataAsync();
        var parentDoc = new Document
        {
            TenantId = tenant.Id,
            DocumentTypeId = docType.Id,
            Title = "V1",
            BlobPath = "/test/v1",
            IsCurrentVersion = false,
            Version = 1,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Documents.AddAsync(parentDoc);
        await _context.SaveChangesAsync();

        var v2 = new Document
        {
            TenantId = tenant.Id,
            DocumentTypeId = docType.Id,
            ParentDocumentId = parentDoc.Id,
            Title = "V2",
            BlobPath = "/test/v2",
            IsCurrentVersion = false,
            Version = 2,
            CreatedAt = DateTime.UtcNow
        };
        var v3 = new Document
        {
            TenantId = tenant.Id,
            DocumentTypeId = docType.Id,
            ParentDocumentId = parentDoc.Id,
            Title = "V3",
            BlobPath = "/test/v3",
            IsCurrentVersion = true,
            Version = 3,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Documents.AddRangeAsync(v2, v3);
        await _context.SaveChangesAsync();

        // Act
        var result = (await _repository.GetVersionsAsync(parentDoc.Id)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(3, result[0].Version);
        Assert.Equal(2, result[1].Version);
    }

    [Fact]
    public async Task GetByContentHashAsync_ReturnsDocumentsWithSameHash()
    {
        // Arrange
        var (tenant, docType) = await SeedTestDataAsync();
        var hash = "abc123def456";
        var doc1 = new Document
        {
            TenantId = tenant.Id,
            DocumentTypeId = docType.Id,
            Title = "Doc 1",
            BlobPath = "/test/doc1",
            ContentHash = hash,
            IsCurrentVersion = true,
            Version = 1,
            CreatedAt = DateTime.UtcNow
        };
        var doc2 = new Document
        {
            TenantId = tenant.Id,
            DocumentTypeId = docType.Id,
            Title = "Doc 2",
            BlobPath = "/test/doc2",
            ContentHash = hash,
            IsCurrentVersion = true,
            Version = 1,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Documents.AddRangeAsync(doc1, doc2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByContentHashAsync(hash);

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task SoftDeleteAsync_MarksDocumentAsDeleted()
    {
        // Arrange
        var (tenant, docType) = await SeedTestDataAsync();
        var document = new Document
        {
            TenantId = tenant.Id,
            DocumentTypeId = docType.Id,
            Title = "To Delete",
            BlobPath = "/test/delete",
            IsCurrentVersion = true,
            Version = 1,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Documents.AddAsync(document);
        await _context.SaveChangesAsync();

        // Act
        await _repository.SoftDeleteAsync(document.Id, "test-user", "Test deletion");
        await _context.SaveChangesAsync();

        // Assert
        var result = await _repository.GetByIdAsync(document.Id);
        Assert.NotNull(result);
        Assert.True(result.IsDeleted);
        Assert.NotNull(result.DeletedAt);
        Assert.Equal("test-user", result.DeletedBy);
        Assert.Equal("Test deletion", result.DeletedReason);
    }

    [Fact]
    public async Task SoftDeleteAsync_WithInvalidId_ThrowsException()
    {
        // Arrange & Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _repository.SoftDeleteAsync(999, "test-user"));
    }

    [Fact]
    public async Task RestoreAsync_RestoresDeletedDocument()
    {
        // Arrange
        var (tenant, docType) = await SeedTestDataAsync();
        var document = new Document
        {
            TenantId = tenant.Id,
            DocumentTypeId = docType.Id,
            Title = "Deleted Doc",
            BlobPath = "/test/deleted",
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow,
            DeletedBy = "test-user",
            IsCurrentVersion = true,
            Version = 1,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Documents.AddAsync(document);
        await _context.SaveChangesAsync();

        // Act
        await _repository.RestoreAsync(document.Id);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _repository.GetByIdAsync(document.Id);
        Assert.NotNull(result);
        Assert.False(result.IsDeleted);
        Assert.Null(result.DeletedAt);
        Assert.Null(result.DeletedBy);
        Assert.Null(result.DeletedReason);
    }

    [Fact]
    public async Task ArchiveAsync_MarksDocumentAsArchived()
    {
        // Arrange
        var (tenant, docType) = await SeedTestDataAsync();
        var document = new Document
        {
            TenantId = tenant.Id,
            DocumentTypeId = docType.Id,
            Title = "To Archive",
            BlobPath = "/test/archive",
            IsCurrentVersion = true,
            Version = 1,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Documents.AddAsync(document);
        await _context.SaveChangesAsync();

        // Act
        await _repository.ArchiveAsync(document.Id);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _repository.GetByIdAsync(document.Id);
        Assert.NotNull(result);
        Assert.True(result.IsArchived);
        Assert.NotNull(result.ArchivedAt);
    }

    [Fact]
    public async Task GetArchivedDocumentsAsync_ReturnsArchivedDocuments()
    {
        // Arrange
        var (tenant, docType) = await SeedTestDataAsync();
        var activeDoc = new Document
        {
            TenantId = tenant.Id,
            DocumentTypeId = docType.Id,
            Title = "Active",
            BlobPath = "/test/active",
            IsArchived = false,
            IsCurrentVersion = true,
            Version = 1,
            CreatedAt = DateTime.UtcNow
        };
        var archivedDoc = new Document
        {
            TenantId = tenant.Id,
            DocumentTypeId = docType.Id,
            Title = "Archived",
            BlobPath = "/test/archived",
            IsArchived = true,
            ArchivedAt = DateTime.UtcNow,
            IsCurrentVersion = true,
            Version = 1,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Documents.AddRangeAsync(activeDoc, archivedDoc);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetArchivedDocumentsAsync(tenant.Id);

        // Assert
        Assert.Single(result);
        Assert.Contains(result, d => d.Title == "Archived");
    }

    [Fact]
    public async Task GetDocumentsNeedingIndexingAsync_ReturnsUnindexedDocuments()
    {
        // Arrange
        var (tenant, docType) = await SeedTestDataAsync();
        var unindexedDoc = new Document
        {
            TenantId = tenant.Id,
            DocumentTypeId = docType.Id,
            Title = "Unindexed",
            BlobPath = "/test/unindexed",
            LastIndexedAt = null,
            IsCurrentVersion = true,
            Version = 1,
            CreatedAt = DateTime.UtcNow
        };
        var indexedDoc = new Document
        {
            TenantId = tenant.Id,
            DocumentTypeId = docType.Id,
            Title = "Indexed",
            BlobPath = "/test/indexed",
            LastIndexedAt = DateTime.UtcNow,
            IsCurrentVersion = true,
            Version = 1,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Documents.AddRangeAsync(unindexedDoc, indexedDoc);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetDocumentsNeedingIndexingAsync();

        // Assert
        Assert.Single(result);
        Assert.Contains(result, d => d.Title == "Unindexed");
    }

    [Fact]
    public async Task AddAsync_AddsDocumentToDatabase()
    {
        // Arrange
        var (tenant, docType) = await SeedTestDataAsync();
        var document = new Document
        {
            TenantId = tenant.Id,
            DocumentTypeId = docType.Id,
            Title = "New Document",
            BlobPath = "/test/new",
            IsCurrentVersion = true,
            Version = 1,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await _repository.AddAsync(document);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _repository.GetByIdAsync(document.Id);
        Assert.NotNull(result);
        Assert.Equal("New Document", result.Title);
    }

    [Fact]
    public async Task Update_UpdatesDocumentInDatabase()
    {
        // Arrange
        var (tenant, docType) = await SeedTestDataAsync();
        var document = new Document
        {
            TenantId = tenant.Id,
            DocumentTypeId = docType.Id,
            Title = "Original Title",
            BlobPath = "/test/original",
            IsCurrentVersion = true,
            Version = 1,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Documents.AddAsync(document);
        await _context.SaveChangesAsync();

        // Act
        document.Title = "Updated Title";
        document.UpdatedAt = DateTime.UtcNow;
        document.UpdatedBy = "test-user";
        _repository.Update(document);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _repository.GetByIdAsync(document.Id);
        Assert.NotNull(result);
        Assert.Equal("Updated Title", result.Title);
        Assert.NotNull(result.UpdatedAt);
        Assert.Equal("test-user", result.UpdatedBy);
    }

    [Fact]
    public async Task CountAsync_ReturnsCorrectCount()
    {
        // Arrange
        var (tenant, docType) = await SeedTestDataAsync();
        var docs = new[]
        {
            new Document { TenantId = tenant.Id, DocumentTypeId = docType.Id, Title = "Doc1", BlobPath = "/test/1", IsDeleted = false, IsCurrentVersion = true, Version = 1, CreatedAt = DateTime.UtcNow },
            new Document { TenantId = tenant.Id, DocumentTypeId = docType.Id, Title = "Doc2", BlobPath = "/test/2", IsDeleted = false, IsCurrentVersion = true, Version = 1, CreatedAt = DateTime.UtcNow },
            new Document { TenantId = tenant.Id, DocumentTypeId = docType.Id, Title = "Doc3", BlobPath = "/test/3", IsDeleted = true, IsCurrentVersion = true, Version = 1, CreatedAt = DateTime.UtcNow }
        };
        await _context.Documents.AddRangeAsync(docs);
        await _context.SaveChangesAsync();

        // Act
        var totalCount = await _repository.CountAsync();
        var activeCount = await _repository.CountAsync(d => !d.IsDeleted);

        // Assert
        Assert.Equal(3, totalCount);
        Assert.Equal(2, activeCount);
    }

    [Fact]
    public async Task AnyAsync_ReturnsCorrectResult()
    {
        // Arrange
        var (tenant, docType) = await SeedTestDataAsync();
        var document = new Document
        {
            TenantId = tenant.Id,
            DocumentTypeId = docType.Id,
            Title = "Test",
            BlobPath = "/test/test",
            IsCurrentVersion = true,
            Version = 1,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Documents.AddAsync(document);
        await _context.SaveChangesAsync();

        // Act
        var exists = await _repository.AnyAsync(d => d.Title == "Test");
        var notExists = await _repository.AnyAsync(d => d.Title == "NonExistent");

        // Assert
        Assert.True(exists);
        Assert.False(notExists);
    }
}
