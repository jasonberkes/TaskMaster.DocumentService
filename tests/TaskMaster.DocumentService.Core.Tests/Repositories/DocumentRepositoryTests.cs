using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Data;
using TaskMaster.DocumentService.Data.Repositories;

namespace TaskMaster.DocumentService.Core.Tests.Repositories;

/// <summary>
/// Unit tests for DocumentRepository.
/// </summary>
public class DocumentRepositoryTests : IDisposable
{
    private readonly DocumentServiceDbContext _context;
    private readonly Mock<ILogger<DocumentRepository>> _loggerMock;
    private readonly DocumentRepository _repository;

    public DocumentRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<DocumentServiceDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new DocumentServiceDbContext(options);
        _loggerMock = new Mock<ILogger<DocumentRepository>>();
        _repository = new DocumentRepository(_context, _loggerMock.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var tenant = new Tenant
        {
            Id = 1,
            TenantType = "Organization",
            Name = "Test Tenant",
            Slug = "test-tenant",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var documentType = new DocumentType
        {
            Id = 1,
            Name = "Invoice",
            DisplayName = "Invoice Document",
            IsContentIndexed = true,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Tenants.Add(tenant);
        _context.DocumentTypes.Add(documentType);
        _context.SaveChanges();
    }

    [Fact]
    public async Task CreateAsync_WithValidDocument_ShouldCreateSuccessfully()
    {
        // Arrange
        var document = new Document
        {
            TenantId = 1,
            DocumentTypeId = 1,
            Title = "Test Document",
            BlobPath = "/blobs/test.pdf"
        };

        // Act
        var result = await _repository.CreateAsync(document);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingDocument_ShouldReturnDocument()
    {
        // Arrange
        var document = new Document
        {
            TenantId = 1,
            DocumentTypeId = 1,
            Title = "Test Document",
            BlobPath = "/blobs/test.pdf"
        };
        await _repository.CreateAsync(document);

        // Act
        var result = await _repository.GetByIdAsync(document.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(document.Id);
        result.Title.Should().Be("Test Document");
        result.DocumentType.Should().NotBeNull();
        result.Tenant.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentDocument_ShouldReturnNull()
    {
        // Arrange
        const long nonExistentId = 999;

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithDeletedDocument_ShouldReturnNull()
    {
        // Arrange
        var document = new Document
        {
            TenantId = 1,
            DocumentTypeId = 1,
            Title = "Deleted Document",
            BlobPath = "/blobs/deleted.pdf",
            IsDeleted = true
        };
        await _repository.CreateAsync(document);

        // Act
        var result = await _repository.GetByIdAsync(document.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByTenantIdAsync_WithExistingDocuments_ShouldReturnList()
    {
        // Arrange
        var documents = new List<Document>
        {
            new Document
            {
                TenantId = 1,
                DocumentTypeId = 1,
                Title = "Document 1",
                BlobPath = "/blobs/doc1.pdf"
            },
            new Document
            {
                TenantId = 1,
                DocumentTypeId = 1,
                Title = "Document 2",
                BlobPath = "/blobs/doc2.pdf"
            }
        };

        foreach (var doc in documents)
        {
            await _repository.CreateAsync(doc);
        }

        // Act
        var result = await _repository.GetByTenantIdAsync(1);

        // Assert
        result.Should().HaveCount(2);
        result.All(d => d.TenantId == 1).Should().BeTrue();
    }

    [Fact]
    public async Task GetByTenantIdAsync_ExcludingArchived_ShouldNotReturnArchivedDocuments()
    {
        // Arrange
        var documents = new List<Document>
        {
            new Document
            {
                TenantId = 1,
                DocumentTypeId = 1,
                Title = "Active Document",
                BlobPath = "/blobs/active.pdf",
                IsArchived = false
            },
            new Document
            {
                TenantId = 1,
                DocumentTypeId = 1,
                Title = "Archived Document",
                BlobPath = "/blobs/archived.pdf",
                IsArchived = true
            }
        };

        foreach (var doc in documents)
        {
            await _repository.CreateAsync(doc);
        }

        // Act
        var result = await _repository.GetByTenantIdAsync(1, includeArchived: false);

        // Assert
        result.Should().HaveCount(1);
        result.First().Title.Should().Be("Active Document");
    }

    [Fact]
    public async Task GetByTenantIdAsync_IncludingArchived_ShouldReturnAllDocuments()
    {
        // Arrange
        var documents = new List<Document>
        {
            new Document
            {
                TenantId = 1,
                DocumentTypeId = 1,
                Title = "Active Document",
                BlobPath = "/blobs/active.pdf",
                IsArchived = false
            },
            new Document
            {
                TenantId = 1,
                DocumentTypeId = 1,
                Title = "Archived Document",
                BlobPath = "/blobs/archived.pdf",
                IsArchived = true
            }
        };

        foreach (var doc in documents)
        {
            await _repository.CreateAsync(doc);
        }

        // Act
        var result = await _repository.GetByTenantIdAsync(1, includeArchived: true);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUnindexedDocumentsAsync_ShouldReturnUnindexedDocuments()
    {
        // Arrange
        var documents = new List<Document>
        {
            new Document
            {
                TenantId = 1,
                DocumentTypeId = 1,
                Title = "Unindexed Document",
                BlobPath = "/blobs/unindexed.pdf",
                MeilisearchId = null
            },
            new Document
            {
                TenantId = 1,
                DocumentTypeId = 1,
                Title = "Indexed Document",
                BlobPath = "/blobs/indexed.pdf",
                MeilisearchId = "1_1",
                LastIndexedAt = DateTime.UtcNow
            }
        };

        foreach (var doc in documents)
        {
            await _repository.CreateAsync(doc);
        }

        // Act
        var result = await _repository.GetUnindexedDocumentsAsync();

        // Assert
        result.Should().HaveCount(1);
        result.First().Title.Should().Be("Unindexed Document");
    }

    [Fact]
    public async Task UpdateAsync_WithValidDocument_ShouldUpdateSuccessfully()
    {
        // Arrange
        var document = new Document
        {
            TenantId = 1,
            DocumentTypeId = 1,
            Title = "Original Title",
            BlobPath = "/blobs/test.pdf"
        };
        await _repository.CreateAsync(document);

        // Act
        document.Title = "Updated Title";
        await _repository.UpdateAsync(document);

        var updated = await _repository.GetByIdAsync(document.Id);

        // Assert
        updated.Should().NotBeNull();
        updated!.Title.Should().Be("Updated Title");
        updated.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithExistingDocument_ShouldSoftDelete()
    {
        // Arrange
        var document = new Document
        {
            TenantId = 1,
            DocumentTypeId = 1,
            Title = "Document to Delete",
            BlobPath = "/blobs/delete.pdf"
        };
        await _repository.CreateAsync(document);

        // Act
        var result = await _repository.DeleteAsync(document.Id, "test-user", "Test deletion");

        // Assert
        result.Should().BeTrue();

        var deleted = await _context.Documents.FindAsync(document.Id);
        deleted.Should().NotBeNull();
        deleted!.IsDeleted.Should().BeTrue();
        deleted.DeletedBy.Should().Be("test-user");
        deleted.DeletedReason.Should().Be("Test deletion");
        deleted.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentDocument_ShouldReturnFalse()
    {
        // Arrange
        const long nonExistentId = 999;

        // Act
        var result = await _repository.DeleteAsync(nonExistentId, "test-user");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WithAlreadyDeletedDocument_ShouldReturnFalse()
    {
        // Arrange
        var document = new Document
        {
            TenantId = 1,
            DocumentTypeId = 1,
            Title = "Already Deleted",
            BlobPath = "/blobs/deleted.pdf",
            IsDeleted = true
        };
        await _repository.CreateAsync(document);

        // Act
        var result = await _repository.DeleteAsync(document.Id, "test-user");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateIndexingInfoAsync_WithValidDocument_ShouldUpdateSuccessfully()
    {
        // Arrange
        var document = new Document
        {
            TenantId = 1,
            DocumentTypeId = 1,
            Title = "Document to Index",
            BlobPath = "/blobs/index.pdf"
        };
        await _repository.CreateAsync(document);

        // Act
        await _repository.UpdateIndexingInfoAsync(document.Id, "1_1");

        var updated = await _context.Documents.FindAsync(document.Id);

        // Assert
        updated.Should().NotBeNull();
        updated!.MeilisearchId.Should().Be("1_1");
        updated.LastIndexedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateIndexingInfoAsync_WithNonExistentDocument_ShouldThrow()
    {
        // Arrange
        const long nonExistentId = 999;

        // Act
        var act = async () => await _repository.UpdateIndexingInfoAsync(nonExistentId, "1_999");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Document {nonExistentId} not found");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
