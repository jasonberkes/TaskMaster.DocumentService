using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using TaskMaster.DocumentService.Api.Controllers;
using TaskMaster.DocumentService.Core.DTOs;
using TaskMaster.DocumentService.Core.Interfaces;

namespace TaskMaster.DocumentService.Api.Tests.Controllers;

/// <summary>
/// Unit tests for DocumentsController.
/// </summary>
public class DocumentsControllerTests
{
    private readonly Mock<IDocumentService> _mockService;
    private readonly Mock<ILogger<DocumentsController>> _mockLogger;
    private readonly DocumentsController _controller;
    private readonly Guid _testTenantId = Guid.NewGuid();

    public DocumentsControllerTests()
    {
        _mockService = new Mock<IDocumentService>();
        _mockLogger = new Mock<ILogger<DocumentsController>>();
        _controller = new DocumentsController(_mockService.Object, _mockLogger.Object);

        // Setup controller context with claims
        var claims = new List<Claim>
        {
            new Claim("TenantId", _testTenantId.ToString()),
            new Claim(ClaimTypes.Name, "test-user")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    [Fact]
    public async Task GetDocuments_ReturnsOkWithDocuments()
    {
        // Arrange
        var documents = new List<DocumentDto>
        {
            new() { Id = Guid.NewGuid(), TenantId = _testTenantId, Name = "Doc1", ContentType = "application/pdf", Size = 100, UploadedBy = "user1" },
            new() { Id = Guid.NewGuid(), TenantId = _testTenantId, Name = "Doc2", ContentType = "text/plain", Size = 200, UploadedBy = "user2" }
        };

        _mockService.Setup(s => s.GetTenantDocumentsAsync(_testTenantId))
            .ReturnsAsync(documents);

        // Act
        var result = await _controller.GetDocuments();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedDocuments = Assert.IsAssignableFrom<IEnumerable<DocumentDto>>(okResult.Value);
        Assert.Equal(2, returnedDocuments.Count());
    }

    [Fact]
    public async Task GetDocument_WithValidId_ReturnsOk()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var document = new DocumentDto
        {
            Id = documentId,
            TenantId = _testTenantId,
            Name = "Test Doc",
            ContentType = "application/pdf",
            Size = 1024,
            UploadedBy = "user"
        };

        _mockService.Setup(s => s.GetDocumentAsync(documentId, _testTenantId))
            .ReturnsAsync(document);

        // Act
        var result = await _controller.GetDocument(documentId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedDocument = Assert.IsType<DocumentDto>(okResult.Value);
        Assert.Equal(documentId, returnedDocument.Id);
    }

    [Fact]
    public async Task GetDocument_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var documentId = Guid.NewGuid();

        _mockService.Setup(s => s.GetDocumentAsync(documentId, _testTenantId))
            .ReturnsAsync((DocumentDto?)null);

        // Act
        var result = await _controller.GetDocument(documentId);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CreateDocument_ReturnsCreatedAtAction()
    {
        // Arrange
        var createDto = new CreateDocumentDto
        {
            Name = "New Doc",
            ContentType = "application/pdf",
            Size = 2048,
            BlobPath = "/path/to/blob"
        };

        var createdDocument = new DocumentDto
        {
            Id = Guid.NewGuid(),
            TenantId = _testTenantId,
            Name = createDto.Name,
            ContentType = createDto.ContentType,
            Size = createDto.Size,
            UploadedBy = "test-user"
        };

        _mockService.Setup(s => s.CreateDocumentAsync(createDto, _testTenantId, "test-user"))
            .ReturnsAsync(createdDocument);

        // Act
        var result = await _controller.CreateDocument(createDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnedDocument = Assert.IsType<DocumentDto>(createdResult.Value);
        Assert.Equal(createdDocument.Id, returnedDocument.Id);
    }

    [Fact]
    public async Task UpdateDocument_WithValidId_ReturnsOk()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var updateDto = new UpdateDocumentDto { Name = "Updated Name" };
        var updatedDocument = new DocumentDto
        {
            Id = documentId,
            TenantId = _testTenantId,
            Name = "Updated Name",
            ContentType = "application/pdf",
            Size = 1024,
            UploadedBy = "user"
        };

        _mockService.Setup(s => s.UpdateDocumentAsync(documentId, updateDto, _testTenantId))
            .ReturnsAsync(updatedDocument);

        // Act
        var result = await _controller.UpdateDocument(documentId, updateDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedDocument = Assert.IsType<DocumentDto>(okResult.Value);
        Assert.Equal("Updated Name", returnedDocument.Name);
    }

    [Fact]
    public async Task UpdateDocument_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var updateDto = new UpdateDocumentDto { Name = "Updated Name" };

        _mockService.Setup(s => s.UpdateDocumentAsync(documentId, updateDto, _testTenantId))
            .ReturnsAsync((DocumentDto?)null);

        // Act
        var result = await _controller.UpdateDocument(documentId, updateDto);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task DeleteDocument_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var documentId = Guid.NewGuid();

        _mockService.Setup(s => s.DeleteDocumentAsync(documentId, _testTenantId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteDocument(documentId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteDocument_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var documentId = Guid.NewGuid();

        _mockService.Setup(s => s.DeleteDocumentAsync(documentId, _testTenantId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteDocument(documentId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}
