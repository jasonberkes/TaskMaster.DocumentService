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
/// Unit tests for InboxProcessorService.
/// </summary>
public class InboxProcessorServiceTests
{
    private readonly Mock<BlobServiceClient> _mockBlobServiceClient;
    private readonly Mock<IDocumentService> _mockDocumentService;
    private readonly Mock<ILogger<InboxProcessorService>> _mockLogger;
    private readonly Mock<IOptions<InboxProcessorOptions>> _mockOptions;
    private readonly InboxProcessorOptions _options;

    public InboxProcessorServiceTests()
    {
        _mockBlobServiceClient = new Mock<BlobServiceClient>();
        _mockDocumentService = new Mock<IDocumentService>();
        _mockLogger = new Mock<ILogger<InboxProcessorService>>();
        _options = new InboxProcessorOptions
        {
            Enabled = true,
            InboxContainerName = "inbox",
            ProcessedContainerName = "processed",
            FailedContainerName = "failed",
            PollingIntervalSeconds = 30,
            BatchSize = 10,
            DefaultTenantId = 1,
            DefaultDocumentTypeId = 1,
            SystemUser = "InboxProcessor"
        };
        _mockOptions = new Mock<IOptions<InboxProcessorOptions>>();
        _mockOptions.Setup(x => x.Value).Returns(_options);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange & Act
        var service = new InboxProcessorService(
            _mockBlobServiceClient.Object,
            _mockDocumentService.Object,
            _mockLogger.Object,
            _mockOptions.Object);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithNullBlobServiceClient_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new InboxProcessorService(null!, _mockDocumentService.Object, _mockLogger.Object, _mockOptions.Object));

        Assert.Equal("blobServiceClient", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullDocumentService_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new InboxProcessorService(_mockBlobServiceClient.Object, null!, _mockLogger.Object, _mockOptions.Object));

        Assert.Equal("documentService", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new InboxProcessorService(_mockBlobServiceClient.Object, _mockDocumentService.Object, null!, _mockOptions.Object));

        Assert.Equal("logger", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new InboxProcessorService(_mockBlobServiceClient.Object, _mockDocumentService.Object, _mockLogger.Object, null!));

        Assert.Equal("options", exception.ParamName);
    }

    #endregion

    #region ProcessInboxFilesAsync Tests

    [Fact]
    public async Task ProcessInboxFilesAsync_WhenDisabled_ShouldReturnZero()
    {
        // Arrange
        _options.Enabled = false;
        var service = new InboxProcessorService(
            _mockBlobServiceClient.Object,
            _mockDocumentService.Object,
            _mockLogger.Object,
            _mockOptions.Object);

        // Act
        var result = await service.ProcessInboxFilesAsync();

        // Assert
        Assert.Equal(0, result);
        _mockBlobServiceClient.Verify(x => x.GetBlobContainerClient(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ProcessInboxFilesAsync_WithNoFiles_ShouldReturnZero()
    {
        // Arrange
        var mockInboxContainer = new Mock<BlobContainerClient>();
        var mockProcessedContainer = new Mock<BlobContainerClient>();
        var mockFailedContainer = new Mock<BlobContainerClient>();

        _mockBlobServiceClient.Setup(x => x.GetBlobContainerClient("inbox"))
            .Returns(mockInboxContainer.Object);
        _mockBlobServiceClient.Setup(x => x.GetBlobContainerClient("processed"))
            .Returns(mockProcessedContainer.Object);
        _mockBlobServiceClient.Setup(x => x.GetBlobContainerClient("failed"))
            .Returns(mockFailedContainer.Object);

        // Setup empty blob list
        var emptyBlobPages = CreateAsyncPageable<BlobItem>(new List<BlobItem>());
        mockInboxContainer.Setup(x => x.GetBlobsAsync(
            It.IsAny<BlobTraits>(),
            It.IsAny<BlobStates>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .Returns(emptyBlobPages);

        var service = new InboxProcessorService(
            _mockBlobServiceClient.Object,
            _mockDocumentService.Object,
            _mockLogger.Object,
            _mockOptions.Object);

        // Act
        var result = await service.ProcessInboxFilesAsync();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ProcessInboxFilesAsync_WithSingleFile_ShouldProcessSuccessfully()
    {
        // Arrange
        var blobName = "test-file.pdf";
        var blobItem = BlobsModelFactory.BlobItem(name: blobName);
        var blobItems = new List<BlobItem> { blobItem };

        var mockInboxContainer = new Mock<BlobContainerClient>();
        var mockProcessedContainer = new Mock<BlobContainerClient>();
        var mockFailedContainer = new Mock<BlobContainerClient>();
        var mockBlobClient = new Mock<BlobClient>();
        var mockDestinationBlobClient = new Mock<BlobClient>();

        _mockBlobServiceClient.Setup(x => x.GetBlobContainerClient("inbox"))
            .Returns(mockInboxContainer.Object);
        _mockBlobServiceClient.Setup(x => x.GetBlobContainerClient("processed"))
            .Returns(mockProcessedContainer.Object);
        _mockBlobServiceClient.Setup(x => x.GetBlobContainerClient("failed"))
            .Returns(mockFailedContainer.Object);

        var blobPages = CreateAsyncPageable(blobItems);
        mockInboxContainer.Setup(x => x.GetBlobsAsync(
            It.IsAny<BlobTraits>(),
            It.IsAny<BlobStates>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .Returns(blobPages);

        mockInboxContainer.Setup(x => x.GetBlobClient(blobName))
            .Returns(mockBlobClient.Object);

        // Setup blob properties with metadata
        var metadata = new Dictionary<string, string>
        {
            ["TenantId"] = "1",
            ["DocumentTypeId"] = "1",
            ["Title"] = "Test Document"
        };
        var properties = BlobsModelFactory.BlobProperties(
            contentType: "application/pdf",
            metadata: metadata);

        var propertiesResponse = Response.FromValue(properties, Mock.Of<Response>());
        mockBlobClient.Setup(x => x.GetPropertiesAsync(
            It.IsAny<BlobRequestConditions>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(propertiesResponse);

        // Setup blob download
        var contentStream = new MemoryStream();
        var downloadResponse = BlobsModelFactory.BlobDownloadStreamingResult(content: contentStream);
        var downloadResult = Response.FromValue(downloadResponse, Mock.Of<Response>());
        mockBlobClient.Setup(x => x.DownloadStreamingAsync(
            It.IsAny<HttpRange>(),
            It.IsAny<BlobRequestConditions>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(downloadResult);

        // Setup document creation
        var document = new Document
        {
            Id = 1,
            TenantId = 1,
            DocumentTypeId = 1,
            Title = "Test Document"
        };
        _mockDocumentService.Setup(x => x.CreateDocumentAsync(
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        // Setup blob copy operation
        mockProcessedContainer.Setup(x => x.GetBlobClient(It.IsAny<string>()))
            .Returns(mockDestinationBlobClient.Object);

        var copyOperation = new MockOperation<long>(123, new ValueTask<Response<long>>(Response.FromValue<long>(123, Mock.Of<Response>())));
        mockDestinationBlobClient.Setup(x => x.StartCopyFromUriAsync(
            It.IsAny<Uri>(),
            It.IsAny<BlobCopyFromUriOptions>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(copyOperation);

        var service = new InboxProcessorService(
            _mockBlobServiceClient.Object,
            _mockDocumentService.Object,
            _mockLogger.Object,
            _mockOptions.Object);

        // Act
        var result = await service.ProcessInboxFilesAsync();

        // Assert
        Assert.Equal(1, result);
        _mockDocumentService.Verify(x => x.CreateDocumentAsync(
            1, // TenantId
            1, // DocumentTypeId
            "Test Document", // Title
            It.IsAny<string>(),
            It.IsAny<Stream>(),
            blobName, // FileName
            "application/pdf", // ContentType
            It.IsAny<string>(),
            It.IsAny<string>(),
            "InboxProcessor", // CreatedBy
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessInboxFilesAsync_WithMultipleFiles_ShouldProcessAllFiles()
    {
        // Arrange
        var blobItems = new List<BlobItem>
        {
            BlobsModelFactory.BlobItem(name: "file1.pdf"),
            BlobsModelFactory.BlobItem(name: "file2.docx"),
            BlobsModelFactory.BlobItem(name: "file3.txt")
        };

        var mockInboxContainer = new Mock<BlobContainerClient>();
        var mockProcessedContainer = new Mock<BlobContainerClient>();
        var mockFailedContainer = new Mock<BlobContainerClient>();

        _mockBlobServiceClient.Setup(x => x.GetBlobContainerClient("inbox"))
            .Returns(mockInboxContainer.Object);
        _mockBlobServiceClient.Setup(x => x.GetBlobContainerClient("processed"))
            .Returns(mockProcessedContainer.Object);
        _mockBlobServiceClient.Setup(x => x.GetBlobContainerClient("failed"))
            .Returns(mockFailedContainer.Object);

        var blobPages = CreateAsyncPageable(blobItems);
        mockInboxContainer.Setup(x => x.GetBlobsAsync(
            It.IsAny<BlobTraits>(),
            It.IsAny<BlobStates>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .Returns(blobPages);

        // Setup each blob
        foreach (var blobItem in blobItems)
        {
            var mockBlobClient = new Mock<BlobClient>();
            var mockDestinationBlobClient = new Mock<BlobClient>();

            mockInboxContainer.Setup(x => x.GetBlobClient(blobItem.Name))
                .Returns(mockBlobClient.Object);

            var metadata = new Dictionary<string, string>();
            var properties = BlobsModelFactory.BlobProperties(contentType: "application/octet-stream", metadata: metadata);
            var propertiesResponse = Response.FromValue(properties, Mock.Of<Response>());
            mockBlobClient.Setup(x => x.GetPropertiesAsync(It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(propertiesResponse);

            var contentStream = new MemoryStream();
            var downloadResponse = BlobsModelFactory.BlobDownloadStreamingResult(content: contentStream);
            var downloadResult = Response.FromValue(downloadResponse, Mock.Of<Response>());
            mockBlobClient.Setup(x => x.DownloadStreamingAsync(
                It.IsAny<HttpRange>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(downloadResult);

            mockProcessedContainer.Setup(x => x.GetBlobClient(It.IsAny<string>()))
                .Returns(mockDestinationBlobClient.Object);

            var copyOperation = new MockOperation<long>(123, new ValueTask<Response<long>>(Response.FromValue<long>(123, Mock.Of<Response>())));
            mockDestinationBlobClient.Setup(x => x.StartCopyFromUriAsync(
                It.IsAny<Uri>(),
                It.IsAny<BlobCopyFromUriOptions>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(copyOperation);
        }

        var document = new Document { Id = 1, TenantId = 1, DocumentTypeId = 1 };
        _mockDocumentService.Setup(x => x.CreateDocumentAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        var service = new InboxProcessorService(
            _mockBlobServiceClient.Object,
            _mockDocumentService.Object,
            _mockLogger.Object,
            _mockOptions.Object);

        // Act
        var result = await service.ProcessInboxFilesAsync();

        // Assert
        Assert.Equal(3, result);
        _mockDocumentService.Verify(x => x.CreateDocumentAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task ProcessInboxFilesAsync_WithBatchSizeLimit_ShouldProcessOnlyBatchSize()
    {
        // Arrange
        _options.BatchSize = 2;

        var blobItems = new List<BlobItem>
        {
            BlobsModelFactory.BlobItem(name: "file1.pdf"),
            BlobsModelFactory.BlobItem(name: "file2.docx"),
            BlobsModelFactory.BlobItem(name: "file3.txt"), // This should not be processed
            BlobsModelFactory.BlobItem(name: "file4.png")  // This should not be processed
        };

        var mockInboxContainer = new Mock<BlobContainerClient>();
        var mockProcessedContainer = new Mock<BlobContainerClient>();
        var mockFailedContainer = new Mock<BlobContainerClient>();

        _mockBlobServiceClient.Setup(x => x.GetBlobContainerClient("inbox"))
            .Returns(mockInboxContainer.Object);
        _mockBlobServiceClient.Setup(x => x.GetBlobContainerClient("processed"))
            .Returns(mockProcessedContainer.Object);
        _mockBlobServiceClient.Setup(x => x.GetBlobContainerClient("failed"))
            .Returns(mockFailedContainer.Object);

        var blobPages = CreateAsyncPageable(blobItems);
        mockInboxContainer.Setup(x => x.GetBlobsAsync(
            It.IsAny<BlobTraits>(),
            It.IsAny<BlobStates>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .Returns(blobPages);

        // Setup first two blobs
        for (int i = 0; i < 2; i++)
        {
            var blobItem = blobItems[i];
            var mockBlobClient = new Mock<BlobClient>();
            var mockDestinationBlobClient = new Mock<BlobClient>();

            mockInboxContainer.Setup(x => x.GetBlobClient(blobItem.Name))
                .Returns(mockBlobClient.Object);

            var metadata = new Dictionary<string, string>();
            var properties = BlobsModelFactory.BlobProperties(contentType: "application/octet-stream", metadata: metadata);
            var propertiesResponse = Response.FromValue(properties, Mock.Of<Response>());
            mockBlobClient.Setup(x => x.GetPropertiesAsync(It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(propertiesResponse);

            var contentStream = new MemoryStream();
            var downloadResponse = BlobsModelFactory.BlobDownloadStreamingResult(content: contentStream);
            var downloadResult = Response.FromValue(downloadResponse, Mock.Of<Response>());
            mockBlobClient.Setup(x => x.DownloadStreamingAsync(
                It.IsAny<HttpRange>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(downloadResult);

            mockProcessedContainer.Setup(x => x.GetBlobClient(It.IsAny<string>()))
                .Returns(mockDestinationBlobClient.Object);

            var copyOperation = new MockOperation<long>(123, new ValueTask<Response<long>>(Response.FromValue<long>(123, Mock.Of<Response>())));
            mockDestinationBlobClient.Setup(x => x.StartCopyFromUriAsync(
                It.IsAny<Uri>(),
                It.IsAny<BlobCopyFromUriOptions>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(copyOperation);
        }

        var document = new Document { Id = 1, TenantId = 1, DocumentTypeId = 1 };
        _mockDocumentService.Setup(x => x.CreateDocumentAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        var service = new InboxProcessorService(
            _mockBlobServiceClient.Object,
            _mockDocumentService.Object,
            _mockLogger.Object,
            _mockOptions.Object);

        // Act
        var result = await service.ProcessInboxFilesAsync();

        // Assert
        Assert.Equal(2, result);
        _mockDocumentService.Verify(x => x.CreateDocumentAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ProcessInboxFilesAsync_WithTenantFolderStructure_ShouldExtractTenantId()
    {
        // Arrange
        var blobName = "tenant-5/document.pdf";
        var blobItem = BlobsModelFactory.BlobItem(name: blobName);
        var blobItems = new List<BlobItem> { blobItem };

        var mockInboxContainer = new Mock<BlobContainerClient>();
        var mockProcessedContainer = new Mock<BlobContainerClient>();
        var mockFailedContainer = new Mock<BlobContainerClient>();
        var mockBlobClient = new Mock<BlobClient>();
        var mockDestinationBlobClient = new Mock<BlobClient>();

        _mockBlobServiceClient.Setup(x => x.GetBlobContainerClient("inbox"))
            .Returns(mockInboxContainer.Object);
        _mockBlobServiceClient.Setup(x => x.GetBlobContainerClient("processed"))
            .Returns(mockProcessedContainer.Object);
        _mockBlobServiceClient.Setup(x => x.GetBlobContainerClient("failed"))
            .Returns(mockFailedContainer.Object);

        var blobPages = CreateAsyncPageable(blobItems);
        mockInboxContainer.Setup(x => x.GetBlobsAsync(
            It.IsAny<BlobTraits>(),
            It.IsAny<BlobStates>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .Returns(blobPages);

        mockInboxContainer.Setup(x => x.GetBlobClient(blobName))
            .Returns(mockBlobClient.Object);

        var metadata = new Dictionary<string, string>();
        var properties = BlobsModelFactory.BlobProperties(contentType: "application/pdf", metadata: metadata);
        var propertiesResponse = Response.FromValue(properties, Mock.Of<Response>());
        mockBlobClient.Setup(x => x.GetPropertiesAsync(It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(propertiesResponse);

        var contentStream = new MemoryStream();
        var downloadResponse = BlobsModelFactory.BlobDownloadStreamingResult(content: contentStream);
        var downloadResult = Response.FromValue(downloadResponse, Mock.Of<Response>());
        mockBlobClient.Setup(x => x.DownloadStreamingAsync(
            It.IsAny<HttpRange>(),
            It.IsAny<BlobRequestConditions>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(downloadResult);

        var document = new Document { Id = 1, TenantId = 5, DocumentTypeId = 1 };
        _mockDocumentService.Setup(x => x.CreateDocumentAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        mockProcessedContainer.Setup(x => x.GetBlobClient(It.IsAny<string>()))
            .Returns(mockDestinationBlobClient.Object);

        var copyOperation = new MockOperation<long>(123, new ValueTask<Response<long>>(Response.FromValue<long>(123, Mock.Of<Response>())));
        mockDestinationBlobClient.Setup(x => x.StartCopyFromUriAsync(
            It.IsAny<Uri>(),
            It.IsAny<BlobCopyFromUriOptions>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(copyOperation);

        var service = new InboxProcessorService(
            _mockBlobServiceClient.Object,
            _mockDocumentService.Object,
            _mockLogger.Object,
            _mockOptions.Object);

        // Act
        var result = await service.ProcessInboxFilesAsync();

        // Assert
        Assert.Equal(1, result);
        _mockDocumentService.Verify(x => x.CreateDocumentAsync(
            5, // TenantId from folder structure
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates an AsyncPageable for testing.
    /// </summary>
    private static AsyncPageable<T> CreateAsyncPageable<T>(IEnumerable<T> items)
    {
        return new MockAsyncPageable<T>(items);
    }

    /// <summary>
    /// Mock implementation of AsyncPageable for testing.
    /// </summary>
    private class MockAsyncPageable<T> : AsyncPageable<T>
    {
        private readonly IEnumerable<T> _items;

        public MockAsyncPageable(IEnumerable<T> items)
        {
            _items = items;
        }

        public override async IAsyncEnumerable<Page<T>> AsPages(string? continuationToken = null, int? pageSizeHint = null)
        {
            await Task.Yield();
            yield return new MockPage<T>(_items);
        }
    }

    /// <summary>
    /// Mock implementation of Page for testing.
    /// </summary>
    private class MockPage<T> : Page<T>
    {
        private readonly IEnumerable<T> _items;

        public MockPage(IEnumerable<T> items)
        {
            _items = items;
        }

        public override IReadOnlyList<T> Values => _items.ToList();
        public override string? ContinuationToken => null;
        public override Response GetRawResponse() => Mock.Of<Response>();
    }

    /// <summary>
    /// Mock implementation of Operation for testing.
    /// </summary>
    private class MockOperation<T> : Azure.Operation<T>
    {
        private readonly T _value;
        private readonly ValueTask<Response<T>> _completionTask;

        public MockOperation(T value, ValueTask<Response<T>> completionTask)
        {
            _value = value;
            _completionTask = completionTask;
        }

        public override bool HasCompleted => true;
        public override bool HasValue => true;
        public override string Id => "mock-operation-id";
        public override T Value => _value;

        public override Response GetRawResponse() => Mock.Of<Response>();
        public override Response UpdateStatus(CancellationToken cancellationToken = default) => Mock.Of<Response>();
        public override ValueTask<Response> UpdateStatusAsync(CancellationToken cancellationToken = default) =>
            new ValueTask<Response>(Mock.Of<Response>());
        public override ValueTask<Response<T>> WaitForCompletionAsync(CancellationToken cancellationToken = default) =>
            _completionTask;
        public override ValueTask<Response<T>> WaitForCompletionAsync(TimeSpan pollingInterval, CancellationToken cancellationToken = default) =>
            _completionTask;
    }

    #endregion
}
