using System.Text;
using System.Text.Json;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Interfaces;
using TaskMaster.DocumentService.Processing.Configuration;
using TaskMaster.DocumentService.Processing.Services;

namespace TaskMaster.DocumentService.Processing.Tests.Services;

/// <summary>
/// Unit tests for <see cref="CodeReviewMigrationService"/>.
/// </summary>
public class CodeReviewMigrationServiceTests
{
    private readonly Mock<BlobServiceClient> _mockBlobServiceClient;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IDocumentService> _mockDocumentService;
    private readonly Mock<ILogger<CodeReviewMigrationService>> _mockLogger;
    private readonly Mock<ICodeReviewRepository> _mockCodeReviewRepository;
    private readonly Mock<IDocumentRepository> _mockDocumentRepository;
    private readonly CodeReviewMigrationOptions _options;

    public CodeReviewMigrationServiceTests()
    {
        _mockBlobServiceClient = new Mock<BlobServiceClient>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockDocumentService = new Mock<IDocumentService>();
        _mockLogger = new Mock<ILogger<CodeReviewMigrationService>>();
        _mockCodeReviewRepository = new Mock<ICodeReviewRepository>();
        _mockDocumentRepository = new Mock<IDocumentRepository>();

        _options = new CodeReviewMigrationOptions
        {
            Enabled = true,
            SourceContainerName = "test-source",
            MigratedContainerName = "test-migrated",
            FailedMigrationContainerName = "test-failed",
            BatchSize = 50,
            DefaultTenantId = 1,
            CodeReviewDocumentTypeId = 2,
            SystemUser = "TestMigration",
            SkipDuplicates = true
        };

        _mockUnitOfWork.Setup(u => u.CodeReviews).Returns(_mockCodeReviewRepository.Object);
        _mockUnitOfWork.Setup(u => u.Documents).Returns(_mockDocumentRepository.Object);
    }

    [Fact]
    public void Constructor_WithNullBlobServiceClient_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CodeReviewMigrationService(
            null!,
            _mockUnitOfWork.Object,
            _mockDocumentService.Object,
            _mockLogger.Object,
            Options.Create(_options)));
    }

    [Fact]
    public void Constructor_WithNullUnitOfWork_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CodeReviewMigrationService(
            _mockBlobServiceClient.Object,
            null!,
            _mockDocumentService.Object,
            _mockLogger.Object,
            Options.Create(_options)));
    }

    [Fact]
    public void Constructor_WithNullDocumentService_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CodeReviewMigrationService(
            _mockBlobServiceClient.Object,
            _mockUnitOfWork.Object,
            null!,
            _mockLogger.Object,
            Options.Create(_options)));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CodeReviewMigrationService(
            _mockBlobServiceClient.Object,
            _mockUnitOfWork.Object,
            _mockDocumentService.Object,
            null!,
            Options.Create(_options)));
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CodeReviewMigrationService(
            _mockBlobServiceClient.Object,
            _mockUnitOfWork.Object,
            _mockDocumentService.Object,
            _mockLogger.Object,
            null!));
    }

    [Fact]
    public async Task MigrateCodeReviewsAsync_WhenDisabled_ReturnsZero()
    {
        // Arrange
        var disabledOptions = new CodeReviewMigrationOptions { Enabled = false };
        var service = new CodeReviewMigrationService(
            _mockBlobServiceClient.Object,
            _mockUnitOfWork.Object,
            _mockDocumentService.Object,
            _mockLogger.Object,
            Options.Create(disabledOptions));

        // Act
        var result = await service.MigrateCodeReviewsAsync();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task GetMigrationStatusAsync_WithValidBatchId_ReturnsStatus()
    {
        // Arrange
        var batchId = "test-batch-001";
        var codeReviews = new List<CodeReview>
        {
            new() { DocumentId = 1, MigrationBatchId = batchId, MigratedAt = DateTime.UtcNow.AddMinutes(-10) },
            new() { DocumentId = 2, MigrationBatchId = batchId, MigratedAt = DateTime.UtcNow.AddMinutes(-5) },
            new() { DocumentId = 3, MigrationBatchId = batchId, MigratedAt = DateTime.UtcNow }
        };

        _mockCodeReviewRepository
            .Setup(r => r.GetByMigrationBatchIdAsync(batchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(codeReviews);

        var service = new CodeReviewMigrationService(
            _mockBlobServiceClient.Object,
            _mockUnitOfWork.Object,
            _mockDocumentService.Object,
            _mockLogger.Object,
            Options.Create(_options));

        // Act
        var result = await service.GetMigrationStatusAsync(batchId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(batchId, result.BatchId);
        Assert.Equal(3, result.TotalProcessed);
        Assert.Equal(3, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);
        Assert.NotNull(result.StartedAt);
        Assert.NotNull(result.CompletedAt);
    }

    [Fact]
    public async Task GetMigrationStatusAsync_WithNoBatchData_ReturnsEmptyStatus()
    {
        // Arrange
        var batchId = "empty-batch";
        _mockCodeReviewRepository
            .Setup(r => r.GetByMigrationBatchIdAsync(batchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CodeReview>());

        var service = new CodeReviewMigrationService(
            _mockBlobServiceClient.Object,
            _mockUnitOfWork.Object,
            _mockDocumentService.Object,
            _mockLogger.Object,
            Options.Create(_options));

        // Act
        var result = await service.GetMigrationStatusAsync(batchId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(batchId, result.BatchId);
        Assert.Equal(0, result.TotalProcessed);
        Assert.Null(result.StartedAt);
        Assert.Null(result.CompletedAt);
    }

    [Fact]
    public void CodeReviewMigrationOptions_HasCorrectDefaults()
    {
        // Arrange
        var options = new CodeReviewMigrationOptions();

        // Assert
        Assert.False(options.Enabled);
        Assert.Equal("platform-code-reviews", options.SourceContainerName);
        Assert.Equal("code-reviews-migrated", options.MigratedContainerName);
        Assert.Equal("code-reviews-migration-failed", options.FailedMigrationContainerName);
        Assert.Equal(50, options.BatchSize);
        Assert.Equal(1, options.DefaultTenantId);
        Assert.Equal(2, options.CodeReviewDocumentTypeId);
        Assert.Equal("CodeReviewMigration", options.SystemUser);
        Assert.True(options.SkipDuplicates);
        Assert.Equal("PullRequestNumber", options.PullRequestNumberMetadataKey);
        Assert.Equal("CodeReviewMetadata", options.MetadataKey);
    }

    [Fact]
    public void MigrationBatchStatus_CanBeInitialized()
    {
        // Arrange & Act
        var status = new MigrationBatchStatus
        {
            BatchId = "test-batch",
            TotalProcessed = 10,
            SuccessCount = 8,
            FailureCount = 2,
            StartedAt = DateTime.UtcNow.AddHours(-1),
            CompletedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal("test-batch", status.BatchId);
        Assert.Equal(10, status.TotalProcessed);
        Assert.Equal(8, status.SuccessCount);
        Assert.Equal(2, status.FailureCount);
        Assert.NotNull(status.StartedAt);
        Assert.NotNull(status.CompletedAt);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CodeReviewMigrationOptions_EnabledFlag_WorksCorrectly(bool enabled)
    {
        // Arrange
        var options = new CodeReviewMigrationOptions { Enabled = enabled };

        // Assert
        Assert.Equal(enabled, options.Enabled);
    }

    [Theory]
    [InlineData("container1")]
    [InlineData("my-container")]
    [InlineData("test-source")]
    public void CodeReviewMigrationOptions_SourceContainerName_CanBeSet(string containerName)
    {
        // Arrange
        var options = new CodeReviewMigrationOptions { SourceContainerName = containerName };

        // Assert
        Assert.Equal(containerName, options.SourceContainerName);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    public void CodeReviewMigrationOptions_BatchSize_CanBeSet(int batchSize)
    {
        // Arrange
        var options = new CodeReviewMigrationOptions { BatchSize = batchSize };

        // Assert
        Assert.Equal(batchSize, options.BatchSize);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void CodeReviewMigrationOptions_DefaultTenantId_CanBeSet(int tenantId)
    {
        // Arrange
        var options = new CodeReviewMigrationOptions { DefaultTenantId = tenantId };

        // Assert
        Assert.Equal(tenantId, options.DefaultTenantId);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    public void CodeReviewMigrationOptions_CodeReviewDocumentTypeId_CanBeSet(int typeId)
    {
        // Arrange
        var options = new CodeReviewMigrationOptions { CodeReviewDocumentTypeId = typeId };

        // Assert
        Assert.Equal(typeId, options.CodeReviewDocumentTypeId);
    }
}
