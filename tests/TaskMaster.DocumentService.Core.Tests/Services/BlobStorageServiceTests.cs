using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TaskMaster.DocumentService.Core.Services;

namespace TaskMaster.DocumentService.Core.Tests.Services;

/// <summary>
/// Unit tests for BlobStorageService.
/// </summary>
public class BlobStorageServiceTests
{
    private readonly Mock<BlobServiceClient> _mockBlobServiceClient;
    private readonly Mock<ILogger<BlobStorageService>> _mockLogger;
    private readonly Mock<IOptions<BlobStorageOptions>> _mockOptions;
    private readonly BlobStorageOptions _options;

    public BlobStorageServiceTests()
    {
        _mockBlobServiceClient = new Mock<BlobServiceClient>();
        _mockLogger = new Mock<ILogger<BlobStorageService>>();
        _options = new BlobStorageOptions
        {
            ConnectionString = "UseDevelopmentStorage=true",
            DefaultContainerName = "test-container"
        };
        _mockOptions = new Mock<IOptions<BlobStorageOptions>>();
        _mockOptions.Setup(x => x.Value).Returns(_options);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange & Act
        var service = new BlobStorageService(
            _mockBlobServiceClient.Object,
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
            new BlobStorageService(null!, _mockLogger.Object, _mockOptions.Object));

        Assert.Equal("blobServiceClient", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new BlobStorageService(_mockBlobServiceClient.Object, null!, _mockOptions.Object));

        Assert.Equal("logger", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new BlobStorageService(_mockBlobServiceClient.Object, _mockLogger.Object, null!));

        Assert.Equal("options", exception.ParamName);
    }

    #endregion

    #region UploadAsync Tests

    [Fact]
    public async Task UploadAsync_WithValidParameters_ShouldReturnBlobUri()
    {
        // Arrange
        var containerName = "test-container";
        var blobName = "test-blob.txt";
        var contentType = "text/plain";
        var content = new MemoryStream();
        var expectedUri = new Uri("https://test.blob.core.windows.net/test-container/test-blob.txt");

        var mockContainerClient = new Mock<BlobContainerClient>();
        var mockBlobClient = new Mock<BlobClient>();

        _mockBlobServiceClient
            .Setup(x => x.GetBlobContainerClient(containerName))
            .Returns(mockContainerClient.Object);

        mockContainerClient
            .Setup(x => x.CreateIfNotExistsAsync(
                It.IsAny<PublicAccessType>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<BlobContainerEncryptionScopeOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue<BlobContainerInfo>(null!, Mock.Of<Response>()));

        mockContainerClient
            .Setup(x => x.GetBlobClient(blobName))
            .Returns(mockBlobClient.Object);

        mockBlobClient.Setup(x => x.Uri).Returns(expectedUri);

        mockBlobClient
            .Setup(x => x.UploadAsync(
                It.IsAny<Stream>(),
                It.IsAny<BlobUploadOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue<BlobContentInfo>(null!, Mock.Of<Response>()));

        var service = new BlobStorageService(
            _mockBlobServiceClient.Object,
            _mockLogger.Object,
            _mockOptions.Object);

        // Act
        var result = await service.UploadAsync(containerName, blobName, content, contentType);

        // Assert
        Assert.Equal(expectedUri.ToString(), result);
        mockBlobClient.Verify(x => x.UploadAsync(
            content,
            It.IsAny<BlobUploadOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadAsync_WithNullContainerName_ShouldThrowArgumentException()
    {
        // Arrange
        var service = new BlobStorageService(
            _mockBlobServiceClient.Object,
            _mockLogger.Object,
            _mockOptions.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await service.UploadAsync(null!, "blob", new MemoryStream(), "text/plain"));
    }

    [Fact]
    public async Task UploadAsync_WithEmptyContainerName_ShouldThrowArgumentException()
    {
        // Arrange
        var service = new BlobStorageService(
            _mockBlobServiceClient.Object,
            _mockLogger.Object,
            _mockOptions.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await service.UploadAsync("", "blob", new MemoryStream(), "text/plain"));
    }

    [Fact]
    public async Task UploadAsync_WithNullBlobName_ShouldThrowArgumentException()
    {
        // Arrange
        var service = new BlobStorageService(
            _mockBlobServiceClient.Object,
            _mockLogger.Object,
            _mockOptions.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await service.UploadAsync("container", null!, new MemoryStream(), "text/plain"));
    }

    [Fact]
    public async Task UploadAsync_WithNullContent_ShouldThrowArgumentNullException()
    {
        // Arrange
        var service = new BlobStorageService(
            _mockBlobServiceClient.Object,
            _mockLogger.Object,
            _mockOptions.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await service.UploadAsync("container", "blob", null!, "text/plain"));
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WhenBlobExists_ShouldReturnTrue()
    {
        // Arrange
        var containerName = "test-container";
        var blobName = "test-blob.txt";

        var mockContainerClient = new Mock<BlobContainerClient>();
        var mockBlobClient = new Mock<BlobClient>();

        _mockBlobServiceClient
            .Setup(x => x.GetBlobContainerClient(containerName))
            .Returns(mockContainerClient.Object);

        mockContainerClient
            .Setup(x => x.GetBlobClient(blobName))
            .Returns(mockBlobClient.Object);

        mockBlobClient
            .Setup(x => x.DeleteIfExistsAsync(
                It.IsAny<DeleteSnapshotsOption>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

        var service = new BlobStorageService(
            _mockBlobServiceClient.Object,
            _mockLogger.Object,
            _mockOptions.Object);

        // Act
        var result = await service.DeleteAsync(containerName, blobName);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteAsync_WhenBlobDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        var containerName = "test-container";
        var blobName = "non-existent-blob.txt";

        var mockContainerClient = new Mock<BlobContainerClient>();
        var mockBlobClient = new Mock<BlobClient>();

        _mockBlobServiceClient
            .Setup(x => x.GetBlobContainerClient(containerName))
            .Returns(mockContainerClient.Object);

        mockContainerClient
            .Setup(x => x.GetBlobClient(blobName))
            .Returns(mockBlobClient.Object);

        mockBlobClient
            .Setup(x => x.DeleteIfExistsAsync(
                It.IsAny<DeleteSnapshotsOption>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

        var service = new BlobStorageService(
            _mockBlobServiceClient.Object,
            _mockLogger.Object,
            _mockOptions.Object);

        // Act
        var result = await service.DeleteAsync(containerName, blobName);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_WithNullContainerName_ShouldThrowArgumentException()
    {
        // Arrange
        var service = new BlobStorageService(
            _mockBlobServiceClient.Object,
            _mockLogger.Object,
            _mockOptions.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await service.DeleteAsync(null!, "blob"));
    }

    [Fact]
    public async Task DeleteAsync_WithNullBlobName_ShouldThrowArgumentException()
    {
        // Arrange
        var service = new BlobStorageService(
            _mockBlobServiceClient.Object,
            _mockLogger.Object,
            _mockOptions.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await service.DeleteAsync("container", null!));
    }

    #endregion

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_WhenBlobExists_ShouldReturnTrue()
    {
        // Arrange
        var containerName = "test-container";
        var blobName = "test-blob.txt";

        var mockContainerClient = new Mock<BlobContainerClient>();
        var mockBlobClient = new Mock<BlobClient>();

        _mockBlobServiceClient
            .Setup(x => x.GetBlobContainerClient(containerName))
            .Returns(mockContainerClient.Object);

        mockContainerClient
            .Setup(x => x.GetBlobClient(blobName))
            .Returns(mockBlobClient.Object);

        mockBlobClient
            .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

        var service = new BlobStorageService(
            _mockBlobServiceClient.Object,
            _mockLogger.Object,
            _mockOptions.Object);

        // Act
        var result = await service.ExistsAsync(containerName, blobName);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_WhenBlobDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        var containerName = "test-container";
        var blobName = "non-existent-blob.txt";

        var mockContainerClient = new Mock<BlobContainerClient>();
        var mockBlobClient = new Mock<BlobClient>();

        _mockBlobServiceClient
            .Setup(x => x.GetBlobContainerClient(containerName))
            .Returns(mockContainerClient.Object);

        mockContainerClient
            .Setup(x => x.GetBlobClient(blobName))
            .Returns(mockBlobClient.Object);

        mockBlobClient
            .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

        var service = new BlobStorageService(
            _mockBlobServiceClient.Object,
            _mockLogger.Object,
            _mockOptions.Object);

        // Act
        var result = await service.ExistsAsync(containerName, blobName);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ExistsAsync_WithNullContainerName_ShouldThrowArgumentException()
    {
        // Arrange
        var service = new BlobStorageService(
            _mockBlobServiceClient.Object,
            _mockLogger.Object,
            _mockOptions.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await service.ExistsAsync(null!, "blob"));
    }

    [Fact]
    public async Task ExistsAsync_WithNullBlobName_ShouldThrowArgumentException()
    {
        // Arrange
        var service = new BlobStorageService(
            _mockBlobServiceClient.Object,
            _mockLogger.Object,
            _mockOptions.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await service.ExistsAsync("container", null!));
    }

    #endregion

    #region ListBlobsAsync Tests

    [Fact]
    public async Task ListBlobsAsync_WithValidContainer_ShouldReturnBlobNames()
    {
        // Arrange
        var containerName = "test-container";

        var mockContainerClient = new Mock<BlobContainerClient>();

        _mockBlobServiceClient
            .Setup(x => x.GetBlobContainerClient(containerName))
            .Returns(mockContainerClient.Object);

        mockContainerClient
            .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

        var blobItems = new[]
        {
            BlobsModelFactory.BlobItem("blob1.txt"),
            BlobsModelFactory.BlobItem("blob2.txt"),
            BlobsModelFactory.BlobItem("blob3.txt")
        };

        mockContainerClient
            .Setup(x => x.GetBlobsAsync(
                It.IsAny<BlobTraits>(),
                It.IsAny<BlobStates>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncPageable(blobItems));

        var service = new BlobStorageService(
            _mockBlobServiceClient.Object,
            _mockLogger.Object,
            _mockOptions.Object);

        // Act
        var result = await service.ListBlobsAsync(containerName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
        Assert.Contains("blob1.txt", result);
        Assert.Contains("blob2.txt", result);
        Assert.Contains("blob3.txt", result);
    }

    [Fact]
    public async Task ListBlobsAsync_WithNonExistentContainer_ShouldReturnEmptyList()
    {
        // Arrange
        var containerName = "non-existent-container";

        var mockContainerClient = new Mock<BlobContainerClient>();

        _mockBlobServiceClient
            .Setup(x => x.GetBlobContainerClient(containerName))
            .Returns(mockContainerClient.Object);

        mockContainerClient
            .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

        var service = new BlobStorageService(
            _mockBlobServiceClient.Object,
            _mockLogger.Object,
            _mockOptions.Object);

        // Act
        var result = await service.ListBlobsAsync(containerName);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ListBlobsAsync_WithNullContainerName_ShouldThrowArgumentException()
    {
        // Arrange
        var service = new BlobStorageService(
            _mockBlobServiceClient.Object,
            _mockLogger.Object,
            _mockOptions.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await service.ListBlobsAsync(null!));
    }

    #endregion

    #region Helper Methods

    private static AsyncPageable<BlobItem> CreateAsyncPageable(IEnumerable<BlobItem> items)
    {
        return new TestAsyncPageable(items);
    }

    private class TestAsyncPageable : AsyncPageable<BlobItem>
    {
        private readonly IEnumerable<BlobItem> _items;

        public TestAsyncPageable(IEnumerable<BlobItem> items)
        {
            _items = items;
        }

        public override async IAsyncEnumerable<Page<BlobItem>> AsPages(string? continuationToken = null, int? pageSizeHint = null)
        {
            await Task.CompletedTask;
            yield return new TestPage(_items);
        }
    }

    private class TestPage : Page<BlobItem>
    {
        private readonly IEnumerable<BlobItem> _items;

        public TestPage(IEnumerable<BlobItem> items)
        {
            _items = items;
        }

        public override IReadOnlyList<BlobItem> Values => _items.ToList();

        public override string? ContinuationToken => null;

        public override Response GetRawResponse() => Mock.Of<Response>();
    }

    #endregion
}
