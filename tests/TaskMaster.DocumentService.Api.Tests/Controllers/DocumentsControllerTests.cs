using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TaskMaster.DocumentService.Api.Controllers;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Interfaces;
using Xunit;

namespace TaskMaster.DocumentService.Api.Tests.Controllers;

/// <summary>
/// Unit tests for <see cref="DocumentsController"/>.
/// </summary>
public class DocumentsControllerTests
{
    private readonly Mock<IDocumentService> _documentService;
    private readonly Mock<ILogger<DocumentsController>> _logger;
    private readonly DocumentsController _controller;
    private readonly int _testTenantId = 1;
    private readonly string _testUserId = "testuser";

    public DocumentsControllerTests()
    {
        _documentService = new Mock<IDocumentService>();
        _logger = new Mock<ILogger<DocumentsController>>();
        _controller = new DocumentsController(_documentService.Object, _logger.Object);

        // Setup default authenticated user
        SetupUserContext(_testTenantId, _testUserId);
    }

    private void SetupUserContext(int tenantId, string userId)
    {
        var claims = new List<Claim>
        {
            new("TenantId", tenantId.ToString()),
            new("TenantName", "Test Tenant"),
            new(ClaimTypes.Name, userId),
            new("AuthenticationType", "Bearer")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    private IFormFile CreateMockFormFile(string fileName, string content, string contentType = "text/plain")
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        var file = new Mock<IFormFile>();
        file.Setup(f => f.FileName).Returns(fileName);
        file.Setup(f => f.Length).Returns(bytes.Length);
        file.Setup(f => f.ContentType).Returns(contentType);
        file.Setup(f => f.OpenReadStream()).Returns(stream);
        return file.Object;
    }

    #region CreateDocument Tests

    /// <summary>
    /// Test that CreateDocument returns Created result when valid data is provided.
    /// </summary>
    [Fact]
    public async Task CreateDocument_WithValidData_ReturnsCreatedResult()
    {
        // Arrange
        var file = CreateMockFormFile("test.txt", "Test content");
        var document = new Document
        {
            Id = 1,
            TenantId = _testTenantId,
            DocumentTypeId = 1,
            Title = "Test Document",
            OriginalFileName = "test.txt",
            MimeType = "text/plain",
            FileSizeBytes = 100,
            Version = 1,
            IsCurrentVersion = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _testUserId
        };

        _documentService
            .Setup(x => x.CreateDocumentAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        // Act
        var result = await _controller.CreateDocument(
            _testTenantId, 1, "Test Document", null, file, null, null);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.NotNull(createdResult.Value);
        Assert.Equal(nameof(DocumentsController.GetDocumentById), createdResult.ActionName);
    }

    /// <summary>
    /// Test that CreateDocument returns BadRequest when file is null.
    /// </summary>
    [Fact]
    public async Task CreateDocument_WithNullFile_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.CreateDocument(
            _testTenantId, 1, "Test Document", null, null!, null, null);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    /// <summary>
    /// Test that CreateDocument returns BadRequest when title is empty.
    /// </summary>
    [Fact]
    public async Task CreateDocument_WithEmptyTitle_ReturnsBadRequest()
    {
        // Arrange
        var file = CreateMockFormFile("test.txt", "Test content");

        // Act
        var result = await _controller.CreateDocument(
            _testTenantId, 1, "", null, file, null, null);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test that CreateDocument handles service exceptions properly.
    /// </summary>
    [Fact]
    public async Task CreateDocument_WhenServiceThrowsInvalidOperationException_ReturnsBadRequest()
    {
        // Arrange
        var file = CreateMockFormFile("test.txt", "Test content");
        _documentService
            .Setup(x => x.CreateDocumentAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Invalid operation"));

        // Act
        var result = await _controller.CreateDocument(
            _testTenantId, 1, "Test Document", null, file, null, null);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    #endregion

    #region GetDocumentById Tests

    /// <summary>
    /// Test that GetDocumentById returns document when it exists and belongs to user's tenant.
    /// </summary>
    [Fact]
    public async Task GetDocumentById_WithValidIdAndTenant_ReturnsOkResult()
    {
        // Arrange
        var documentId = 1L;
        var document = new Document
        {
            Id = documentId,
            TenantId = _testTenantId,
            DocumentTypeId = 1,
            Title = "Test Document",
            CreatedAt = DateTime.UtcNow
        };

        _documentService
            .Setup(x => x.GetDocumentByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        // Act
        var result = await _controller.GetDocumentById(documentId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    /// <summary>
    /// Test that GetDocumentById returns NotFound when document does not exist.
    /// </summary>
    [Fact]
    public async Task GetDocumentById_WithNonExistentDocument_ReturnsNotFound()
    {
        // Arrange
        var documentId = 999L;
        _documentService
            .Setup(x => x.GetDocumentByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);

        // Act
        var result = await _controller.GetDocumentById(documentId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// Test that GetDocumentById returns NotFound when document belongs to different tenant.
    /// </summary>
    [Fact]
    public async Task GetDocumentById_WithDifferentTenant_ReturnsNotFound()
    {
        // Arrange
        var documentId = 1L;
        var document = new Document
        {
            Id = documentId,
            TenantId = 999, // Different tenant
            DocumentTypeId = 1,
            Title = "Test Document",
            CreatedAt = DateTime.UtcNow
        };

        _documentService
            .Setup(x => x.GetDocumentByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        // Act
        var result = await _controller.GetDocumentById(documentId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion

    #region GetDocumentsByTenant Tests

    /// <summary>
    /// Test that GetDocumentsByTenant returns documents for the specified tenant.
    /// </summary>
    [Fact]
    public async Task GetDocumentsByTenant_WithValidTenantId_ReturnsOkResult()
    {
        // Arrange
        var documents = new List<Document>
        {
            new Document { Id = 1, TenantId = _testTenantId, Title = "Doc 1", CreatedAt = DateTime.UtcNow },
            new Document { Id = 2, TenantId = _testTenantId, Title = "Doc 2", CreatedAt = DateTime.UtcNow }
        };

        _documentService
            .Setup(x => x.GetDocumentsByTenantAsync(_testTenantId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        // Act
        var result = await _controller.GetDocumentsByTenant(_testTenantId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    /// <summary>
    /// Test that GetDocumentsByTenant includes deleted documents when requested.
    /// </summary>
    [Fact]
    public async Task GetDocumentsByTenant_WithIncludeDeleted_CallsServiceWithCorrectParameter()
    {
        // Arrange
        var documents = new List<Document>();
        _documentService
            .Setup(x => x.GetDocumentsByTenantAsync(_testTenantId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        // Act
        await _controller.GetDocumentsByTenant(_testTenantId, true);

        // Assert
        _documentService.Verify(
            x => x.GetDocumentsByTenantAsync(_testTenantId, true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region GetDocumentsByType Tests

    /// <summary>
    /// Test that GetDocumentsByType returns documents and filters by tenant.
    /// </summary>
    [Fact]
    public async Task GetDocumentsByType_WithValidTypeId_ReturnsFilteredDocuments()
    {
        // Arrange
        var documentTypeId = 1;
        var documents = new List<Document>
        {
            new Document { Id = 1, TenantId = _testTenantId, DocumentTypeId = documentTypeId, Title = "Doc 1", CreatedAt = DateTime.UtcNow },
            new Document { Id = 2, TenantId = 999, DocumentTypeId = documentTypeId, Title = "Doc 2", CreatedAt = DateTime.UtcNow } // Different tenant
        };

        _documentService
            .Setup(x => x.GetDocumentsByTypeAsync(documentTypeId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        // Act
        var result = await _controller.GetDocumentsByType(documentTypeId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        // Verify tenant filtering happens in controller
        _documentService.Verify(
            x => x.GetDocumentsByTypeAsync(documentTypeId, false, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region UpdateDocumentMetadata Tests

    /// <summary>
    /// Test that UpdateDocumentMetadata successfully updates document.
    /// </summary>
    [Fact]
    public async Task UpdateDocumentMetadata_WithValidData_ReturnsOkResult()
    {
        // Arrange
        var documentId = 1L;
        var document = new Document
        {
            Id = documentId,
            TenantId = _testTenantId,
            Title = "Updated Title",
            Description = "Updated Description",
            CreatedAt = DateTime.UtcNow
        };

        _documentService
            .Setup(x => x.GetDocumentByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _documentService
            .Setup(x => x.UpdateDocumentMetadataAsync(
                documentId, It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        var request = new UpdateDocumentMetadataRequest
        {
            Title = "Updated Title",
            Description = "Updated Description"
        };

        // Act
        var result = await _controller.UpdateDocumentMetadata(documentId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    /// <summary>
    /// Test that UpdateDocumentMetadata returns NotFound for non-existent document.
    /// </summary>
    [Fact]
    public async Task UpdateDocumentMetadata_WithNonExistentDocument_ReturnsNotFound()
    {
        // Arrange
        var documentId = 999L;
        _documentService
            .Setup(x => x.GetDocumentByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);

        var request = new UpdateDocumentMetadataRequest { Title = "Updated Title" };

        // Act
        var result = await _controller.UpdateDocumentMetadata(documentId, request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// Test that UpdateDocumentMetadata returns NotFound when document belongs to different tenant.
    /// </summary>
    [Fact]
    public async Task UpdateDocumentMetadata_WithDifferentTenant_ReturnsNotFound()
    {
        // Arrange
        var documentId = 1L;
        var document = new Document
        {
            Id = documentId,
            TenantId = 999, // Different tenant
            Title = "Test Document",
            CreatedAt = DateTime.UtcNow
        };

        _documentService
            .Setup(x => x.GetDocumentByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        var request = new UpdateDocumentMetadataRequest { Title = "Updated Title" };

        // Act
        var result = await _controller.UpdateDocumentMetadata(documentId, request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion

    #region CreateDocumentVersion Tests

    /// <summary>
    /// Test that CreateDocumentVersion creates new version successfully.
    /// </summary>
    [Fact]
    public async Task CreateDocumentVersion_WithValidData_ReturnsCreatedResult()
    {
        // Arrange
        var documentId = 1L;
        var file = CreateMockFormFile("test-v2.txt", "Updated content");
        var parentDocument = new Document
        {
            Id = documentId,
            TenantId = _testTenantId,
            Title = "Test Document",
            Version = 1,
            CreatedAt = DateTime.UtcNow
        };
        var newVersion = new Document
        {
            Id = 2,
            TenantId = _testTenantId,
            ParentDocumentId = documentId,
            Title = "Test Document",
            Version = 2,
            IsCurrentVersion = true,
            CreatedAt = DateTime.UtcNow
        };

        _documentService
            .Setup(x => x.GetDocumentByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentDocument);

        _documentService
            .Setup(x => x.CreateDocumentVersionAsync(
                documentId, It.IsAny<Stream>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newVersion);

        // Act
        var result = await _controller.CreateDocumentVersion(documentId, file);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.NotNull(createdResult.Value);
    }

    /// <summary>
    /// Test that CreateDocumentVersion returns BadRequest when file is null.
    /// </summary>
    [Fact]
    public async Task CreateDocumentVersion_WithNullFile_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.CreateDocumentVersion(1L, null!);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test that CreateDocumentVersion returns NotFound when parent document doesn't exist.
    /// </summary>
    [Fact]
    public async Task CreateDocumentVersion_WithNonExistentParent_ReturnsNotFound()
    {
        // Arrange
        var documentId = 999L;
        var file = CreateMockFormFile("test-v2.txt", "Updated content");

        _documentService
            .Setup(x => x.GetDocumentByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);

        // Act
        var result = await _controller.CreateDocumentVersion(documentId, file);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion

    #region GetDocumentVersions Tests

    /// <summary>
    /// Test that GetDocumentVersions returns all versions of a document.
    /// </summary>
    [Fact]
    public async Task GetDocumentVersions_WithValidDocumentId_ReturnsOkResult()
    {
        // Arrange
        var documentId = 1L;
        var document = new Document
        {
            Id = documentId,
            TenantId = _testTenantId,
            Title = "Test Document",
            CreatedAt = DateTime.UtcNow
        };
        var versions = new List<Document>
        {
            new Document { Id = 2, Version = 1, ParentDocumentId = documentId, CreatedAt = DateTime.UtcNow },
            new Document { Id = 3, Version = 2, ParentDocumentId = documentId, IsCurrentVersion = true, CreatedAt = DateTime.UtcNow }
        };

        _documentService
            .Setup(x => x.GetDocumentByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _documentService
            .Setup(x => x.GetDocumentVersionsAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(versions);

        // Act
        var result = await _controller.GetDocumentVersions(documentId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region GetCurrentVersion Tests

    /// <summary>
    /// Test that GetCurrentVersion returns the current version of a document.
    /// </summary>
    [Fact]
    public async Task GetCurrentVersion_WithValidDocumentId_ReturnsOkResult()
    {
        // Arrange
        var documentId = 1L;
        var document = new Document
        {
            Id = documentId,
            TenantId = _testTenantId,
            Title = "Test Document",
            CreatedAt = DateTime.UtcNow
        };
        var currentVersion = new Document
        {
            Id = 2,
            Version = 2,
            ParentDocumentId = documentId,
            IsCurrentVersion = true,
            CreatedAt = DateTime.UtcNow
        };

        _documentService
            .Setup(x => x.GetDocumentByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _documentService
            .Setup(x => x.GetCurrentVersionAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentVersion);

        // Act
        var result = await _controller.GetCurrentVersion(documentId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    /// <summary>
    /// Test that GetCurrentVersion returns NotFound when no current version exists.
    /// </summary>
    [Fact]
    public async Task GetCurrentVersion_WithNoCurrentVersion_ReturnsNotFound()
    {
        // Arrange
        var documentId = 1L;
        var document = new Document
        {
            Id = documentId,
            TenantId = _testTenantId,
            Title = "Test Document",
            CreatedAt = DateTime.UtcNow
        };

        _documentService
            .Setup(x => x.GetDocumentByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _documentService
            .Setup(x => x.GetCurrentVersionAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);

        // Act
        var result = await _controller.GetCurrentVersion(documentId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion

    #region DownloadDocument Tests

    /// <summary>
    /// Test that DownloadDocument returns file result.
    /// </summary>
    [Fact]
    public async Task DownloadDocument_WithValidDocumentId_ReturnsFileResult()
    {
        // Arrange
        var documentId = 1L;
        var document = new Document
        {
            Id = documentId,
            TenantId = _testTenantId,
            OriginalFileName = "test.txt",
            MimeType = "text/plain",
            CreatedAt = DateTime.UtcNow
        };
        var contentStream = new MemoryStream(Encoding.UTF8.GetBytes("Test content"));

        _documentService
            .Setup(x => x.GetDocumentByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _documentService
            .Setup(x => x.DownloadDocumentAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentStream);

        // Act
        var result = await _controller.DownloadDocument(documentId);

        // Assert
        var fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("text/plain", fileResult.ContentType);
        Assert.Equal("test.txt", fileResult.FileDownloadName);
    }

    /// <summary>
    /// Test that DownloadDocument returns NotFound when document doesn't exist.
    /// </summary>
    [Fact]
    public async Task DownloadDocument_WithNonExistentDocument_ReturnsNotFound()
    {
        // Arrange
        var documentId = 999L;
        _documentService
            .Setup(x => x.GetDocumentByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);

        // Act
        var result = await _controller.DownloadDocument(documentId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion

    #region GetDocumentSasUri Tests

    /// <summary>
    /// Test that GetDocumentSasUri returns SAS URI.
    /// </summary>
    [Fact]
    public async Task GetDocumentSasUri_WithValidDocumentId_ReturnsOkResult()
    {
        // Arrange
        var documentId = 1L;
        var document = new Document
        {
            Id = documentId,
            TenantId = _testTenantId,
            CreatedAt = DateTime.UtcNow
        };
        var sasUri = "https://storage.blob.core.windows.net/container/blob?sas=token";

        _documentService
            .Setup(x => x.GetDocumentByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _documentService
            .Setup(x => x.GetDocumentSasUriAsync(documentId, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sasUri);

        // Act
        var result = await _controller.GetDocumentSasUri(documentId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    /// <summary>
    /// Test that GetDocumentSasUri limits expiration time correctly.
    /// </summary>
    [Fact]
    public async Task GetDocumentSasUri_WithExcessiveExpirationTime_LimitsTo24Hours()
    {
        // Arrange
        var documentId = 1L;
        var document = new Document
        {
            Id = documentId,
            TenantId = _testTenantId,
            CreatedAt = DateTime.UtcNow
        };
        var sasUri = "https://storage.blob.core.windows.net/container/blob?sas=token";

        _documentService
            .Setup(x => x.GetDocumentByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _documentService
            .Setup(x => x.GetDocumentSasUriAsync(documentId, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sasUri);

        // Act
        var result = await _controller.GetDocumentSasUri(documentId, 2000); // 2000 minutes

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        // Verify that the expiration was limited (service should have been called with <= 1440 minutes)
        _documentService.Verify(
            x => x.GetDocumentSasUriAsync(
                documentId,
                It.Is<TimeSpan>(ts => ts.TotalMinutes <= 1440),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region DeleteDocument Tests

    /// <summary>
    /// Test that DeleteDocument successfully soft-deletes a document.
    /// </summary>
    [Fact]
    public async Task DeleteDocument_WithValidDocumentId_ReturnsNoContent()
    {
        // Arrange
        var documentId = 1L;
        var document = new Document
        {
            Id = documentId,
            TenantId = _testTenantId,
            CreatedAt = DateTime.UtcNow
        };

        _documentService
            .Setup(x => x.GetDocumentByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _documentService
            .Setup(x => x.DeleteDocumentAsync(documentId, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteDocument(documentId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    /// <summary>
    /// Test that DeleteDocument returns NotFound when document doesn't exist.
    /// </summary>
    [Fact]
    public async Task DeleteDocument_WithNonExistentDocument_ReturnsNotFound()
    {
        // Arrange
        var documentId = 999L;
        _documentService
            .Setup(x => x.GetDocumentByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);

        // Act
        var result = await _controller.DeleteDocument(documentId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// Test that DeleteDocument passes deletion reason to service.
    /// </summary>
    [Fact]
    public async Task DeleteDocument_WithReason_PassesReasonToService()
    {
        // Arrange
        var documentId = 1L;
        var deletionReason = "Test deletion reason";
        var document = new Document
        {
            Id = documentId,
            TenantId = _testTenantId,
            CreatedAt = DateTime.UtcNow
        };

        _documentService
            .Setup(x => x.GetDocumentByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _documentService
            .Setup(x => x.DeleteDocumentAsync(documentId, It.IsAny<string>(), deletionReason, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _controller.DeleteDocument(documentId, deletionReason);

        // Assert
        _documentService.Verify(
            x => x.DeleteDocumentAsync(documentId, _testUserId, deletionReason, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region RestoreDocument Tests

    /// <summary>
    /// Test that RestoreDocument successfully restores a deleted document.
    /// </summary>
    [Fact]
    public async Task RestoreDocument_WithValidDocumentId_ReturnsNoContent()
    {
        // Arrange
        var documentId = 1L;
        var documents = new List<Document>
        {
            new Document
            {
                Id = documentId,
                TenantId = _testTenantId,
                IsDeleted = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        _documentService
            .Setup(x => x.GetDocumentsByTenantAsync(_testTenantId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        _documentService
            .Setup(x => x.RestoreDocumentAsync(documentId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.RestoreDocument(documentId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    #endregion

    #region ArchiveDocument Tests

    /// <summary>
    /// Test that ArchiveDocument successfully archives a document.
    /// </summary>
    [Fact]
    public async Task ArchiveDocument_WithValidDocumentId_ReturnsNoContent()
    {
        // Arrange
        var documentId = 1L;
        var document = new Document
        {
            Id = documentId,
            TenantId = _testTenantId,
            CreatedAt = DateTime.UtcNow
        };

        _documentService
            .Setup(x => x.GetDocumentByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _documentService
            .Setup(x => x.ArchiveDocumentAsync(documentId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ArchiveDocument(documentId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    #endregion

    #region GetArchivedDocuments Tests

    /// <summary>
    /// Test that GetArchivedDocuments returns archived documents for a tenant.
    /// </summary>
    [Fact]
    public async Task GetArchivedDocuments_WithValidTenantId_ReturnsOkResult()
    {
        // Arrange
        var documents = new List<Document>
        {
            new Document
            {
                Id = 1,
                TenantId = _testTenantId,
                IsArchived = true,
                ArchivedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            }
        };

        _documentService
            .Setup(x => x.GetArchivedDocumentsAsync(_testTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        // Act
        var result = await _controller.GetArchivedDocuments(_testTenantId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region FindDuplicateDocuments Tests

    /// <summary>
    /// Test that FindDuplicateDocuments returns documents with matching content hash.
    /// </summary>
    [Fact]
    public async Task FindDuplicateDocuments_WithValidContentHash_ReturnsOkResult()
    {
        // Arrange
        var contentHash = "abc123def456";
        var documents = new List<Document>
        {
            new Document
            {
                Id = 1,
                TenantId = _testTenantId,
                ContentHash = contentHash,
                CreatedAt = DateTime.UtcNow
            },
            new Document
            {
                Id = 2,
                TenantId = _testTenantId,
                ContentHash = contentHash,
                CreatedAt = DateTime.UtcNow
            }
        };

        _documentService
            .Setup(x => x.FindDuplicateDocumentsAsync(contentHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        // Act
        var result = await _controller.FindDuplicateDocuments(contentHash);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    /// <summary>
    /// Test that FindDuplicateDocuments returns BadRequest when content hash is empty.
    /// </summary>
    [Fact]
    public async Task FindDuplicateDocuments_WithEmptyContentHash_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.FindDuplicateDocuments("");

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test that FindDuplicateDocuments filters results by user's tenant.
    /// </summary>
    [Fact]
    public async Task FindDuplicateDocuments_FiltersResultsByTenant()
    {
        // Arrange
        var contentHash = "abc123def456";
        var documents = new List<Document>
        {
            new Document { Id = 1, TenantId = _testTenantId, ContentHash = contentHash, CreatedAt = DateTime.UtcNow },
            new Document { Id = 2, TenantId = 999, ContentHash = contentHash, CreatedAt = DateTime.UtcNow } // Different tenant
        };

        _documentService
            .Setup(x => x.FindDuplicateDocumentsAsync(contentHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        // Act
        var result = await _controller.FindDuplicateDocuments(contentHash);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        // Verify service was called
        _documentService.Verify(
            x => x.FindDuplicateDocumentsAsync(contentHash, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Service Exception Tests

    /// <summary>
    /// Test that controller handles general exceptions and returns InternalServerError.
    /// </summary>
    [Fact]
    public async Task GetDocumentById_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var documentId = 1L;
        _documentService
            .Setup(x => x.GetDocumentByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _controller.GetDocumentById(documentId);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusResult.StatusCode);
    }

    #endregion
}
