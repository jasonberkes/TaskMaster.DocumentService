using Microsoft.EntityFrameworkCore;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Data;
using TaskMaster.DocumentService.Data.Repositories;

namespace TaskMaster.DocumentService.Core.Tests.Repositories;

/// <summary>
/// Unit tests for <see cref="CodeReviewRepository"/>.
/// </summary>
public class CodeReviewRepositoryTests : IDisposable
{
    private readonly DocumentServiceDbContext _context;
    private readonly CodeReviewRepository _repository;

    public CodeReviewRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<DocumentServiceDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new DocumentServiceDbContext(options);
        _repository = new CodeReviewRepository(_context);

        // Seed test data
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
            Id = 2,
            Name = "CodeReview",
            DisplayName = "Code Review",
            Description = "Code review documents",
            HasExtensionTable = true,
            ExtensionTableName = "CodeReviews",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Tenants.Add(tenant);
        _context.DocumentTypes.Add(documentType);

        var document1 = new Document
        {
            Id = 1,
            TenantId = 1,
            DocumentTypeId = 2,
            Title = "PR #123 - Add new feature",
            BlobPath = "code-reviews/pr-123.json",
            CreatedAt = DateTime.UtcNow,
            IsCurrentVersion = true,
            Version = 1
        };

        var document2 = new Document
        {
            Id = 2,
            TenantId = 1,
            DocumentTypeId = 2,
            Title = "PR #456 - Fix bug",
            BlobPath = "code-reviews/pr-456.json",
            CreatedAt = DateTime.UtcNow,
            IsCurrentVersion = true,
            Version = 1
        };

        _context.Documents.AddRange(document1, document2);

        var codeReview1 = new CodeReview
        {
            DocumentId = 1,
            PullRequestNumber = 123,
            RepositoryName = "TaskMaster.Platform",
            BranchName = "feature/new-feature",
            BaseBranchName = "main",
            Author = "developer1",
            Status = "Approved",
            MigrationBatchId = "batch-001",
            MigratedAt = DateTime.UtcNow
        };

        var codeReview2 = new CodeReview
        {
            DocumentId = 2,
            PullRequestNumber = 456,
            RepositoryName = "TaskMaster.Platform",
            BranchName = "bugfix/critical-fix",
            BaseBranchName = "main",
            Author = "developer2",
            Status = "Pending",
            MigrationBatchId = "batch-001",
            MigratedAt = DateTime.UtcNow
        };

        _context.CodeReviews.AddRange(codeReview1, codeReview2);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsCodeReview()
    {
        // Act
        var result = await _repository.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.DocumentId);
        Assert.Equal(123, result.PullRequestNumber);
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
    public async Task GetByPullRequestNumberAsync_WithValidPRNumber_ReturnsCodeReview()
    {
        // Act
        var result = await _repository.GetByPullRequestNumberAsync(123);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.DocumentId);
        Assert.Equal(123, result.PullRequestNumber);
        Assert.NotNull(result.Document);
        Assert.Equal("PR #123 - Add new feature", result.Document.Title);
    }

    [Fact]
    public async Task GetByPullRequestNumberAsync_WithInvalidPRNumber_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByPullRequestNumberAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByMigrationBatchIdAsync_WithValidBatchId_ReturnsCodeReviews()
    {
        // Act
        var results = await _repository.GetByMigrationBatchIdAsync("batch-001");

        // Assert
        var resultList = results.ToList();
        Assert.Equal(2, resultList.Count);
        Assert.All(resultList, cr => Assert.Equal("batch-001", cr.MigrationBatchId));
    }

    [Fact]
    public async Task GetByMigrationBatchIdAsync_WithInvalidBatchId_ReturnsEmpty()
    {
        // Act
        var results = await _repository.GetByMigrationBatchIdAsync("batch-999");

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetByStatusAsync_WithValidStatus_ReturnsCodeReviews()
    {
        // Act
        var results = await _repository.GetByStatusAsync("Approved");

        // Assert
        var resultList = results.ToList();
        Assert.Single(resultList);
        Assert.Equal("Approved", resultList[0].Status);
        Assert.Equal(123, resultList[0].PullRequestNumber);
    }

    [Fact]
    public async Task GetByStatusAsync_WithInvalidStatus_ReturnsEmpty()
    {
        // Act
        var results = await _repository.GetByStatusAsync("Rejected");

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetByRepositoryAsync_WithValidRepository_ReturnsCodeReviews()
    {
        // Act
        var results = await _repository.GetByRepositoryAsync("TaskMaster.Platform");

        // Assert
        var resultList = results.ToList();
        Assert.Equal(2, resultList.Count);
        Assert.All(resultList, cr => Assert.Equal("TaskMaster.Platform", cr.RepositoryName));
    }

    [Fact]
    public async Task GetByRepositoryAsync_WithInvalidRepository_ReturnsEmpty()
    {
        // Act
        var results = await _repository.GetByRepositoryAsync("NonExistentRepo");

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task AddAsync_WithValidCodeReview_AddsSuccessfully()
    {
        // Arrange
        var document = new Document
        {
            Id = 3,
            TenantId = 1,
            DocumentTypeId = 2,
            Title = "PR #789 - New test",
            BlobPath = "code-reviews/pr-789.json",
            CreatedAt = DateTime.UtcNow,
            IsCurrentVersion = true,
            Version = 1
        };
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        var newCodeReview = new CodeReview
        {
            DocumentId = 3,
            PullRequestNumber = 789,
            RepositoryName = "TaskMaster.API",
            BranchName = "feature/test",
            BaseBranchName = "develop",
            Author = "developer3",
            Status = "Pending",
            MigrationBatchId = "batch-002",
            MigratedAt = DateTime.UtcNow
        };

        // Act
        var result = await _repository.AddAsync(newCodeReview);
        await _context.SaveChangesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.DocumentId);

        var retrieved = await _repository.GetByIdAsync(3);
        Assert.NotNull(retrieved);
        Assert.Equal(789, retrieved.PullRequestNumber);
    }

    [Fact]
    public async Task CountAsync_WithPredicate_ReturnsCorrectCount()
    {
        // Act
        var count = await _repository.CountAsync(cr => cr.Status == "Approved");

        // Assert
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task CountAsync_WithoutPredicate_ReturnsAllCount()
    {
        // Act
        var count = await _repository.CountAsync();

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task AnyAsync_WithMatchingPredicate_ReturnsTrue()
    {
        // Act
        var exists = await _repository.AnyAsync(cr => cr.PullRequestNumber == 123);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task AnyAsync_WithNonMatchingPredicate_ReturnsFalse()
    {
        // Act
        var exists = await _repository.AnyAsync(cr => cr.PullRequestNumber == 999);

        // Assert
        Assert.False(exists);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
