using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using TaskMaster.DocumentService.Core.DTOs;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Interfaces;
using TaskMaster.DocumentService.Core.Services;
using Xunit;

namespace TaskMaster.DocumentService.Core.Tests.Services;

/// <summary>
/// Unit tests for CodeReviewMigrationService.
/// WI #2298: Document Service: Migrate Existing Code Reviews from TaskMaster.Platform
/// </summary>
public class CodeReviewMigrationServiceTests
{
    private readonly Mock<IDocumentRepository> _mockDocumentRepository;
    private readonly Mock<IDocumentTypeRepository> _mockDocumentTypeRepository;
    private readonly Mock<ITenantRepository> _mockTenantRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<CodeReviewMigrationService>> _mockLogger;
    private readonly CodeReviewMigrationService _service;

    public CodeReviewMigrationServiceTests()
    {
        _mockDocumentRepository = new Mock<IDocumentRepository>();
        _mockDocumentTypeRepository = new Mock<IDocumentTypeRepository>();
        _mockTenantRepository = new Mock<ITenantRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<CodeReviewMigrationService>>();

        _service = new CodeReviewMigrationService(
            _mockDocumentRepository.Object,
            _mockDocumentTypeRepository.Object,
            _mockTenantRepository.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange & Act
        var service = new CodeReviewMigrationService(
            _mockDocumentRepository.Object,
            _mockDocumentTypeRepository.Object,
            _mockTenantRepository.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithNullDocumentRepository_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CodeReviewMigrationService(
                null!,
                _mockDocumentTypeRepository.Object,
                _mockTenantRepository.Object,
                _mockUnitOfWork.Object,
                _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullDocumentTypeRepository_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CodeReviewMigrationService(
                _mockDocumentRepository.Object,
                null!,
                _mockTenantRepository.Object,
                _mockUnitOfWork.Object,
                _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullTenantRepository_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CodeReviewMigrationService(
                _mockDocumentRepository.Object,
                _mockDocumentTypeRepository.Object,
                null!,
                _mockUnitOfWork.Object,
                _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullUnitOfWork_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CodeReviewMigrationService(
                _mockDocumentRepository.Object,
                _mockDocumentTypeRepository.Object,
                _mockTenantRepository.Object,
                null!,
                _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CodeReviewMigrationService(
                _mockDocumentRepository.Object,
                _mockDocumentTypeRepository.Object,
                _mockTenantRepository.Object,
                _mockUnitOfWork.Object,
                null!));
    }

    #endregion

    #region ValidateTenantAsync Tests

    [Fact]
    public async Task ValidateTenantAsync_WithValidActiveTenant_ReturnsTrue()
    {
        // Arrange
        var tenantId = 1;
        var tenant = new Tenant { Id = tenantId, IsActive = true };
        _mockTenantRepository
            .Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        // Act
        var result = await _service.ValidateTenantAsync(tenantId);

        // Assert
        Assert.True(result);
        _mockTenantRepository.Verify(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ValidateTenantAsync_WithInactiveTenant_ReturnsFalse()
    {
        // Arrange
        var tenantId = 1;
        var tenant = new Tenant { Id = tenantId, IsActive = false };
        _mockTenantRepository
            .Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        // Act
        var result = await _service.ValidateTenantAsync(tenantId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateTenantAsync_WithNonExistentTenant_ReturnsFalse()
    {
        // Arrange
        var tenantId = 999;
        _mockTenantRepository
            .Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        // Act
        var result = await _service.ValidateTenantAsync(tenantId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateTenantAsync_WhenExceptionThrown_ReturnsFalse()
    {
        // Arrange
        var tenantId = 1;
        _mockTenantRepository
            .Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _service.ValidateTenantAsync(tenantId);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region EnsureCodeReviewDocumentTypeAsync Tests

    [Fact]
    public async Task EnsureCodeReviewDocumentTypeAsync_WhenTypeExists_ReturnsExistingId()
    {
        // Arrange
        var existingType = new DocumentType
        {
            Id = 5,
            Name = "CodeReview",
            DisplayName = "Code Review"
        };

        _mockDocumentTypeRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DocumentType> { existingType });

        // Act
        var result = await _service.EnsureCodeReviewDocumentTypeAsync();

        // Assert
        Assert.Equal(5, result);
        _mockDocumentTypeRepository.Verify(x => x.AddAsync(It.IsAny<DocumentType>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnsureCodeReviewDocumentTypeAsync_WhenTypeDoesNotExist_CreatesNewType()
    {
        // Arrange
        _mockDocumentTypeRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DocumentType>());

        _mockDocumentTypeRepository
            .Setup(x => x.AddAsync(It.IsAny<DocumentType>(), It.IsAny<CancellationToken>()))
            .Callback<DocumentType, CancellationToken>((dt, ct) => dt.Id = 10)
            .ReturnsAsync((DocumentType dt, CancellationToken ct) => dt);

        // Act
        var result = await _service.EnsureCodeReviewDocumentTypeAsync();

        // Assert
        Assert.Equal(10, result);
        _mockDocumentTypeRepository.Verify(x => x.AddAsync(It.IsAny<DocumentType>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region MigrateCodeReviewsAsync Tests

    [Fact]
    public async Task MigrateCodeReviewsAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.MigrateCodeReviewsAsync(null!));
    }

    [Fact]
    public async Task MigrateCodeReviewsAsync_WithNullCodeReviews_ThrowsArgumentException()
    {
        // Arrange
        var request = new MigrateCodeReviewsRequest
        {
            TenantId = 1,
            CodeReviews = null
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.MigrateCodeReviewsAsync(request));
    }

    [Fact]
    public async Task MigrateCodeReviewsAsync_WithEmptyCodeReviews_ThrowsArgumentException()
    {
        // Arrange
        var request = new MigrateCodeReviewsRequest
        {
            TenantId = 1,
            CodeReviews = new List<CodeReviewMigrationDto>()
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.MigrateCodeReviewsAsync(request));
    }

    [Fact]
    public async Task MigrateCodeReviewsAsync_WithInvalidTenant_ReturnsErrorResponse()
    {
        // Arrange
        var request = new MigrateCodeReviewsRequest
        {
            TenantId = 999,
            CodeReviews = new List<CodeReviewMigrationDto>
            {
                CreateSampleCodeReview(1)
            }
        };

        _mockTenantRepository
            .Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        // Act
        var result = await _service.MigrateCodeReviewsAsync(request);

        // Assert
        Assert.Equal(0, result.MigratedCount);
        Assert.Single(result.Errors);
        Assert.Contains("Tenant with ID 999", result.Errors[0]);
    }

    [Fact]
    public async Task MigrateCodeReviewsAsync_WithValidRequest_MigratesSuccessfully()
    {
        // Arrange
        var tenant = new Tenant { Id = 1, IsActive = true };
        var documentType = new DocumentType { Id = 5, Name = "CodeReview" };

        var request = new MigrateCodeReviewsRequest
        {
            TenantId = 1,
            BatchSize = 10,
            SkipExisting = false,
            CodeReviews = new List<CodeReviewMigrationDto>
            {
                CreateSampleCodeReview(1),
                CreateSampleCodeReview(2),
                CreateSampleCodeReview(3)
            }
        };

        _mockTenantRepository
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        _mockDocumentTypeRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DocumentType> { documentType });

        _mockDocumentRepository
            .Setup(x => x.GetByTenantIdAsync(1, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Document>());

        // Act
        var result = await _service.MigrateCodeReviewsAsync(request);

        // Assert
        Assert.Equal(3, result.MigratedCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Empty(result.Errors);
        _mockDocumentRepository.Verify(x => x.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task MigrateCodeReviewsAsync_WithSkipExisting_SkipsAlreadyMigrated()
    {
        // Arrange
        var tenant = new Tenant { Id = 1, IsActive = true };
        var documentType = new DocumentType { Id = 5, Name = "CodeReview" };

        var existingDocument = new Document
        {
            Id = 100,
            TenantId = 1,
            BlobPath = "code-reviews/2025/11/123/20251129-120000-review.json",
            Metadata = "{\"sourceCodeReviewId\":1}"
        };

        var request = new MigrateCodeReviewsRequest
        {
            TenantId = 1,
            SkipExisting = true,
            CodeReviews = new List<CodeReviewMigrationDto>
            {
                CreateSampleCodeReview(1, blobPath: "code-reviews/2025/11/123/20251129-120000-review.json"),
                CreateSampleCodeReview(2)
            }
        };

        _mockTenantRepository
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        _mockDocumentTypeRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DocumentType> { documentType });

        _mockDocumentRepository
            .Setup(x => x.GetByTenantIdAsync(1, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Document> { existingDocument });

        // Act
        var result = await _service.MigrateCodeReviewsAsync(request);

        // Assert
        Assert.Equal(1, result.MigratedCount);
        Assert.Equal(1, result.SkippedCount);
        Assert.Equal(0, result.FailedCount);
    }

    [Fact]
    public async Task MigrateCodeReviewsAsync_WithBatching_ProcessesInBatches()
    {
        // Arrange
        var tenant = new Tenant { Id = 1, IsActive = true };
        var documentType = new DocumentType { Id = 5, Name = "CodeReview" };

        var codeReviews = Enumerable.Range(1, 25)
            .Select(i => CreateSampleCodeReview(i))
            .ToList();

        var request = new MigrateCodeReviewsRequest
        {
            TenantId = 1,
            BatchSize = 10,
            SkipExisting = false,
            CodeReviews = codeReviews
        };

        _mockTenantRepository
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        _mockDocumentTypeRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DocumentType> { documentType });

        _mockDocumentRepository
            .Setup(x => x.GetByTenantIdAsync(1, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Document>());

        // Act
        var result = await _service.MigrateCodeReviewsAsync(request);

        // Assert
        Assert.Equal(25, result.MigratedCount);
        Assert.Equal(0, result.FailedCount);
        _mockDocumentRepository.Verify(x => x.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()), Times.Exactly(25));
    }

    [Fact]
    public async Task MigrateCodeReviewsAsync_WithPartialFailures_ContinuesProcessing()
    {
        // Arrange
        var tenant = new Tenant { Id = 1, IsActive = true };
        var documentType = new DocumentType { Id = 5, Name = "CodeReview" };

        var request = new MigrateCodeReviewsRequest
        {
            TenantId = 1,
            CodeReviews = new List<CodeReviewMigrationDto>
            {
                CreateSampleCodeReview(1),
                CreateSampleCodeReview(2),
                CreateSampleCodeReview(3)
            }
        };

        _mockTenantRepository
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        _mockDocumentTypeRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DocumentType> { documentType });

        _mockDocumentRepository
            .Setup(x => x.GetByTenantIdAsync(1, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Document>());

        // Simulate failure on second item
        var callCount = 0;
        _mockDocumentRepository
            .Setup(x => x.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                callCount++;
                if (callCount == 2)
                    throw new Exception("Simulated error");
            });

        // Act
        var result = await _service.MigrateCodeReviewsAsync(request);

        // Assert
        Assert.Equal(2, result.MigratedCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Single(result.Errors);
    }

    [Fact]
    public async Task MigrateCodeReviewsAsync_WithHighQualityReview_AddsHighQualityTag()
    {
        // Arrange
        var tenant = new Tenant { Id = 1, IsActive = true };
        var documentType = new DocumentType { Id = 5, Name = "CodeReview" };

        var request = new MigrateCodeReviewsRequest
        {
            TenantId = 1,
            CodeReviews = new List<CodeReviewMigrationDto>
            {
                CreateSampleCodeReview(1, qualityScore: 95)
            }
        };

        _mockTenantRepository
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        _mockDocumentTypeRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DocumentType> { documentType });

        _mockDocumentRepository
            .Setup(x => x.GetByTenantIdAsync(1, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Document>());

        Document? capturedDocument = null;
        _mockDocumentRepository
            .Setup(x => x.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
            .Callback<Document, CancellationToken>((doc, ct) => capturedDocument = doc);

        // Act
        var result = await _service.MigrateCodeReviewsAsync(request);

        // Assert
        Assert.Equal(1, result.MigratedCount);
        Assert.NotNull(capturedDocument);
        Assert.Contains("high-quality", capturedDocument.Tags);
    }

    #endregion

    #region Helper Methods

    private CodeReviewMigrationDto CreateSampleCodeReview(
        int id,
        int qualityScore = 85,
        string? blobPath = null)
    {
        return new CodeReviewMigrationDto
        {
            SourceCodeReviewId = id,
            PrNumber = 100 + id,
            GitHubRepoOwner = "test-org",
            GitHubRepoName = "test-repo",
            WorkItemId = 1000 + id,
            ReviewedAt = DateTime.UtcNow.AddDays(-id),
            QualityScore = qualityScore,
            TestStatus = "passing",
            BuildStatus = "success",
            BreakingChanges = false,
            Recommendation = "APPROVE",
            PositiveFindings = "[\"Good test coverage\"]",
            PotentialConcerns = "[]",
            Suggestions = "[]",
            SecurityIssues = "[]",
            PerformanceIssues = "[]",
            InputTokens = 1000,
            OutputTokens = 500,
            ApiCost = 0.05m,
            BlobPath = blobPath ?? $"code-reviews/2025/11/{100 + id}/20251129-120000-review.json",
            CreatedAt = DateTime.UtcNow.AddDays(-id)
        };
    }

    #endregion
}
