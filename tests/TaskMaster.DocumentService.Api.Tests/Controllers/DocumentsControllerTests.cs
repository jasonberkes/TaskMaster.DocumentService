using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TaskMaster.DocumentService.Api.Controllers;
using TaskMaster.DocumentService.Api.Models;
using TaskMaster.DocumentService.Core.Interfaces;

namespace TaskMaster.DocumentService.Api.Tests.Controllers;

/// <summary>
/// Unit tests for DocumentsController.
/// </summary>
public class DocumentsControllerTests
{
    private readonly Mock<IBlobStorageService> _mockBlobStorageService;
    private readonly Mock<ILogger<DocumentsController>> _mockLogger;
    private readonly DocumentsController _controller;

    public DocumentsControllerTests()
    {
        _mockBlobStorageService = new Mock<IBlobStorageService>();
        _mockLogger = new Mock<ILogger<DocumentsController>>();
        _controller = new DocumentsController(_mockBlobStorageService.Object, _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange & Act
        var controller = new DocumentsController(_mockBlobStorageService.Object, _mockLogger.Object);

        // Assert
        Assert.NotNull(controller);
    }

    [Fact]
    public void Constructor_WithNullBlobStorageService_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new DocumentsController(null!, _mockLogger.Object));

        Assert.Equal("blobStorageService", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new DocumentsController(_mockBlobStorageService.Object, null!));

        Assert.Equal("logger", exception.ParamName);
    }

    #endregion

    #region UploadDocument Tests

    [Fact]
    public async Task UploadDocument_WithValidRequest_ShouldReturnOkWithUploadResponse()
    {
        // Arrange
        var containerName = "test-container";
        var blobName = "test-document.pdf";
        var contentType = "application/pdf";
        var expectedUri = "https://test.blob.core.windows.net/test-container/test-document.pdf";

        var mockFile = new Mock<IFormFile>();
        var content = "Test file content";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));

        mockFile.Setup(f => f.OpenReadStream()).Returns(stream);
        mockFile.Setup(f => f.Length).Returns(stream.Length);
        mockFile.Setup(f => f.ContentType).Returns(contentType);

        var request = new DocumentUploadRequest
        {
            ContainerName = containerName,
            BlobName = blobName,
            File = mockFile.Object
        };

        _mockBlobStorageService
            .Setup(s => s.UploadAsync(
                containerName,
                blobName,
                It.IsAny<Stream>(),
                contentType,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUri);

        // Act
        var result = await _controller.UploadDocument(request);

        // Assert
        var okResult = Assert.IsType<ActionResult<DocumentUploadResponse>>(result);
        var okObjectResult = Assert.IsType<OkObjectResult>(okResult.Result);
        var response = Assert.IsType<DocumentUploadResponse>(okObjectResult.Value);

        Assert.Equal(expectedUri, response.Uri);
        Assert.Equal(containerName, response.ContainerName);
        Assert.Equal(blobName, response.BlobName);
        Assert.True((DateTime.UtcNow - response.UploadedAt).TotalSeconds < 5);

        _mockBlobStorageService.Verify(s => s.UploadAsync(
            containerName,
            blobName,
            It.IsAny<Stream>(),
            contentType,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadDocument_WithNullFile_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new DocumentUploadRequest
        {
            ContainerName = "test-container",
            BlobName = "test.pdf",
            File = null
        };

        // Act
        var result = await _controller.UploadDocument(request);

        // Assert
        var actionResult = Assert.IsType<ActionResult<DocumentUploadResponse>>(result);
        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task UploadDocument_WithEmptyFile_ShouldReturnBadRequest()
    {
        // Arrange
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(0);

        var request = new DocumentUploadRequest
        {
            ContainerName = "test-container",
            BlobName = "test.pdf",
            File = mockFile.Object
        };

        // Act
        var result = await _controller.UploadDocument(request);

        // Assert
        var actionResult = Assert.IsType<ActionResult<DocumentUploadResponse>>(result);
        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task UploadDocument_WithEmptyContainerName_ShouldReturnBadRequest()
    {
        // Arrange
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(100);

        var request = new DocumentUploadRequest
        {
            ContainerName = "",
            BlobName = "test.pdf",
            File = mockFile.Object
        };

        // Act
        var result = await _controller.UploadDocument(request);

        // Assert
        var actionResult = Assert.IsType<ActionResult<DocumentUploadResponse>>(result);
        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task UploadDocument_WithEmptyBlobName_ShouldReturnBadRequest()
    {
        // Arrange
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(100);

        var request = new DocumentUploadRequest
        {
            ContainerName = "test-container",
            BlobName = "",
            File = mockFile.Object
        };

        // Act
        var result = await _controller.UploadDocument(request);

        // Assert
        var actionResult = Assert.IsType<ActionResult<DocumentUploadResponse>>(result);
        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task UploadDocument_WhenServiceThrowsException_ShouldReturnInternalServerError()
    {
        // Arrange
        var mockFile = new Mock<IFormFile>();
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("test"));
        mockFile.Setup(f => f.OpenReadStream()).Returns(stream);
        mockFile.Setup(f => f.Length).Returns(stream.Length);
        mockFile.Setup(f => f.ContentType).Returns("text/plain");

        var request = new DocumentUploadRequest
        {
            ContainerName = "test-container",
            BlobName = "test.txt",
            File = mockFile.Object
        };

        _mockBlobStorageService
            .Setup(s => s.UploadAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Storage error"));

        // Act
        var result = await _controller.UploadDocument(request);

        // Assert
        var actionResult = Assert.IsType<ActionResult<DocumentUploadResponse>>(result);
        var objectResult = Assert.IsType<ObjectResult>(actionResult.Result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
    }

    #endregion

    #region DownloadDocument Tests

    [Fact]
    public async Task DownloadDocument_WithValidParameters_ShouldReturnFileStreamResult()
    {
        // Arrange
        var containerName = "test-container";
        var blobName = "test-document.pdf";
        var content = "Test file content";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));

        _mockBlobStorageService
            .Setup(s => s.DownloadAsync(containerName, blobName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stream);

        // Act
        var result = await _controller.DownloadDocument(containerName, blobName);

        // Assert
        var fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("application/octet-stream", fileResult.ContentType);
        Assert.Equal(blobName, fileResult.FileDownloadName);

        _mockBlobStorageService.Verify(s => s.DownloadAsync(
            containerName,
            blobName,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DownloadDocument_WhenFileNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var containerName = "test-container";
        var blobName = "non-existent.pdf";

        _mockBlobStorageService
            .Setup(s => s.DownloadAsync(containerName, blobName, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileNotFoundException("File not found"));

        // Act
        var result = await _controller.DownloadDocument(containerName, blobName);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DownloadDocument_WhenServiceThrowsException_ShouldReturnInternalServerError()
    {
        // Arrange
        var containerName = "test-container";
        var blobName = "test.pdf";

        _mockBlobStorageService
            .Setup(s => s.DownloadAsync(containerName, blobName, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Storage error"));

        // Act
        var result = await _controller.DownloadDocument(containerName, blobName);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
    }

    #endregion

    #region DeleteDocument Tests

    [Fact]
    public async Task DeleteDocument_WhenDocumentExists_ShouldReturnNoContent()
    {
        // Arrange
        var containerName = "test-container";
        var blobName = "test-document.pdf";

        _mockBlobStorageService
            .Setup(s => s.DeleteAsync(containerName, blobName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteDocument(containerName, blobName);

        // Assert
        Assert.IsType<NoContentResult>(result);

        _mockBlobStorageService.Verify(s => s.DeleteAsync(
            containerName,
            blobName,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteDocument_WhenDocumentDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var containerName = "test-container";
        var blobName = "non-existent.pdf";

        _mockBlobStorageService
            .Setup(s => s.DeleteAsync(containerName, blobName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteDocument(containerName, blobName);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteDocument_WhenServiceThrowsException_ShouldReturnInternalServerError()
    {
        // Arrange
        var containerName = "test-container";
        var blobName = "test.pdf";

        _mockBlobStorageService
            .Setup(s => s.DeleteAsync(containerName, blobName, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Storage error"));

        // Act
        var result = await _controller.DeleteDocument(containerName, blobName);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
    }

    #endregion

    #region CheckDocumentExists Tests

    [Fact]
    public async Task CheckDocumentExists_WhenDocumentExists_ShouldReturnOkWithTrueExists()
    {
        // Arrange
        var containerName = "test-container";
        var blobName = "test-document.pdf";

        _mockBlobStorageService
            .Setup(s => s.ExistsAsync(containerName, blobName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.CheckDocumentExists(containerName, blobName);

        // Assert
        var actionResult = Assert.IsType<ActionResult<DocumentInfoResponse>>(result);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<DocumentInfoResponse>(okResult.Value);

        Assert.Equal(blobName, response.BlobName);
        Assert.True(response.Exists);

        _mockBlobStorageService.Verify(s => s.ExistsAsync(
            containerName,
            blobName,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckDocumentExists_WhenDocumentDoesNotExist_ShouldReturnOkWithFalseExists()
    {
        // Arrange
        var containerName = "test-container";
        var blobName = "non-existent.pdf";

        _mockBlobStorageService
            .Setup(s => s.ExistsAsync(containerName, blobName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.CheckDocumentExists(containerName, blobName);

        // Assert
        var actionResult = Assert.IsType<ActionResult<DocumentInfoResponse>>(result);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<DocumentInfoResponse>(okResult.Value);

        Assert.Equal(blobName, response.BlobName);
        Assert.False(response.Exists);
    }

    [Fact]
    public async Task CheckDocumentExists_WhenServiceThrowsException_ShouldReturnInternalServerError()
    {
        // Arrange
        var containerName = "test-container";
        var blobName = "test.pdf";

        _mockBlobStorageService
            .Setup(s => s.ExistsAsync(containerName, blobName, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Storage error"));

        // Act
        var result = await _controller.CheckDocumentExists(containerName, blobName);

        // Assert
        var actionResult = Assert.IsType<ActionResult<DocumentInfoResponse>>(result);
        var objectResult = Assert.IsType<ObjectResult>(actionResult.Result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
    }

    #endregion

    #region GenerateSasUri Tests

    [Fact]
    public async Task GenerateSasUri_WithValidRequest_ShouldReturnOkWithSasUri()
    {
        // Arrange
        var request = new SasUriRequest
        {
            ContainerName = "test-container",
            BlobName = "test-document.pdf",
            ExpiresInHours = 2
        };
        var expectedSasUri = "https://test.blob.core.windows.net/test-container/test-document.pdf?sv=2021-08-06&se=...";

        _mockBlobStorageService
            .Setup(s => s.GetSasUriAsync(
                request.ContainerName,
                request.BlobName,
                TimeSpan.FromHours(request.ExpiresInHours),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSasUri);

        // Act
        var result = await _controller.GenerateSasUri(request);

        // Assert
        var actionResult = Assert.IsType<ActionResult<SasUriResponse>>(result);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<SasUriResponse>(okResult.Value);

        Assert.Equal(expectedSasUri, response.SasUri);
        Assert.True((response.ExpiresAt - DateTime.UtcNow).TotalHours >= 1.9);

        _mockBlobStorageService.Verify(s => s.GetSasUriAsync(
            request.ContainerName,
            request.BlobName,
            TimeSpan.FromHours(request.ExpiresInHours),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateSasUri_WithEmptyContainerName_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new SasUriRequest
        {
            ContainerName = "",
            BlobName = "test.pdf",
            ExpiresInHours = 1
        };

        // Act
        var result = await _controller.GenerateSasUri(request);

        // Assert
        var actionResult = Assert.IsType<ActionResult<SasUriResponse>>(result);
        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task GenerateSasUri_WithEmptyBlobName_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new SasUriRequest
        {
            ContainerName = "test-container",
            BlobName = "",
            ExpiresInHours = 1
        };

        // Act
        var result = await _controller.GenerateSasUri(request);

        // Assert
        var actionResult = Assert.IsType<ActionResult<SasUriResponse>>(result);
        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task GenerateSasUri_WithZeroExpiresInHours_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new SasUriRequest
        {
            ContainerName = "test-container",
            BlobName = "test.pdf",
            ExpiresInHours = 0
        };

        // Act
        var result = await _controller.GenerateSasUri(request);

        // Assert
        var actionResult = Assert.IsType<ActionResult<SasUriResponse>>(result);
        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task GenerateSasUri_WithNegativeExpiresInHours_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new SasUriRequest
        {
            ContainerName = "test-container",
            BlobName = "test.pdf",
            ExpiresInHours = -1
        };

        // Act
        var result = await _controller.GenerateSasUri(request);

        // Assert
        var actionResult = Assert.IsType<ActionResult<SasUriResponse>>(result);
        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task GenerateSasUri_WhenFileNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var request = new SasUriRequest
        {
            ContainerName = "test-container",
            BlobName = "non-existent.pdf",
            ExpiresInHours = 1
        };

        _mockBlobStorageService
            .Setup(s => s.GetSasUriAsync(
                request.ContainerName,
                request.BlobName,
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileNotFoundException("File not found"));

        // Act
        var result = await _controller.GenerateSasUri(request);

        // Assert
        var actionResult = Assert.IsType<ActionResult<SasUriResponse>>(result);
        Assert.IsType<NotFoundObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task GenerateSasUri_WhenServiceThrowsException_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = new SasUriRequest
        {
            ContainerName = "test-container",
            BlobName = "test.pdf",
            ExpiresInHours = 1
        };

        _mockBlobStorageService
            .Setup(s => s.GetSasUriAsync(
                request.ContainerName,
                request.BlobName,
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Storage error"));

        // Act
        var result = await _controller.GenerateSasUri(request);

        // Assert
        var actionResult = Assert.IsType<ActionResult<SasUriResponse>>(result);
        var objectResult = Assert.IsType<ObjectResult>(actionResult.Result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
    }

    #endregion

    #region ListDocuments Tests

    [Fact]
    public async Task ListDocuments_WithValidContainerName_ShouldReturnOkWithDocumentList()
    {
        // Arrange
        var containerName = "test-container";
        var expectedDocuments = new List<string>
        {
            "document1.pdf",
            "document2.pdf",
            "document3.pdf"
        };

        _mockBlobStorageService
            .Setup(s => s.ListBlobsAsync(containerName, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDocuments);

        // Act
        var result = await _controller.ListDocuments(containerName);

        // Assert
        var actionResult = Assert.IsType<ActionResult<IEnumerable<string>>>(result);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var documents = Assert.IsAssignableFrom<IEnumerable<string>>(okResult.Value);

        Assert.Equal(3, documents.Count());
        Assert.Contains("document1.pdf", documents);
        Assert.Contains("document2.pdf", documents);
        Assert.Contains("document3.pdf", documents);

        _mockBlobStorageService.Verify(s => s.ListBlobsAsync(
            containerName,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListDocuments_WithPrefix_ShouldReturnFilteredDocuments()
    {
        // Arrange
        var containerName = "test-container";
        var prefix = "invoices/";
        var expectedDocuments = new List<string>
        {
            "invoices/invoice1.pdf",
            "invoices/invoice2.pdf"
        };

        _mockBlobStorageService
            .Setup(s => s.ListBlobsAsync(containerName, prefix, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDocuments);

        // Act
        var result = await _controller.ListDocuments(containerName, prefix);

        // Assert
        var actionResult = Assert.IsType<ActionResult<IEnumerable<string>>>(result);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var documents = Assert.IsAssignableFrom<IEnumerable<string>>(okResult.Value);

        Assert.Equal(2, documents.Count());

        _mockBlobStorageService.Verify(s => s.ListBlobsAsync(
            containerName,
            prefix,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListDocuments_WithEmptyContainer_ShouldReturnEmptyList()
    {
        // Arrange
        var containerName = "empty-container";

        _mockBlobStorageService
            .Setup(s => s.ListBlobsAsync(containerName, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _controller.ListDocuments(containerName);

        // Assert
        var actionResult = Assert.IsType<ActionResult<IEnumerable<string>>>(result);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var documents = Assert.IsAssignableFrom<IEnumerable<string>>(okResult.Value);

        Assert.Empty(documents);
    }

    [Fact]
    public async Task ListDocuments_WhenServiceThrowsException_ShouldReturnInternalServerError()
    {
        // Arrange
        var containerName = "test-container";

        _mockBlobStorageService
            .Setup(s => s.ListBlobsAsync(containerName, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Storage error"));

        // Act
        var result = await _controller.ListDocuments(containerName);

        // Assert
        var actionResult = Assert.IsType<ActionResult<IEnumerable<string>>>(result);
        var objectResult = Assert.IsType<ObjectResult>(actionResult.Result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
    }

    #endregion
}
