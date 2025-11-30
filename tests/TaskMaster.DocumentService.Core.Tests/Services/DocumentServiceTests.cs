using Microsoft.Extensions.Logging;
using Moq;
using TaskMaster.DocumentService.Core.DTOs;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Interfaces;
using TaskMaster.DocumentService.Core.Services;

namespace TaskMaster.DocumentService.Core.Tests.Services;

/// <summary>
/// Unit tests for DocumentService.
/// </summary>
public class DocumentServiceTests
{
    private readonly Mock<IDocumentRepository> _mockRepository;
    private readonly Mock<ILogger<Core.Services.DocumentService>> _mockLogger;
    private readonly Core.Services.DocumentService _service;

    public DocumentServiceTests()
    {
        _mockRepository = new Mock<IDocumentRepository>();
        _mockLogger = new Mock<ILogger<Core.Services.DocumentService>>();
        _service = new Core.Services.DocumentService(_mockRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetDocumentAsync_WithValidId_ReturnsDocument()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var document = new Document
        {
            Id = documentId,
            TenantId = tenantId,
            Name = "Test Document",
            ContentType = "application/pdf",
            Size = 1024,
            BlobPath = "/path/to/blob",
            UploadedBy = "user123"
        };

        _mockRepository.Setup(r => r.GetByIdAsync(documentId, tenantId))
            .ReturnsAsync(document);

        // Act
        var result = await _service.GetDocumentAsync(documentId, tenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(documentId, result.Id);
        Assert.Equal(tenantId, result.TenantId);
        Assert.Equal("Test Document", result.Name);
        _mockRepository.Verify(r => r.GetByIdAsync(documentId, tenantId), Times.Once);
    }

    [Fact]
    public async Task GetDocumentAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var documentId = Guid.NewGuid();

        _mockRepository.Setup(r => r.GetByIdAsync(documentId, tenantId))
            .ReturnsAsync((Document?)null);

        // Act
        var result = await _service.GetDocumentAsync(documentId, tenantId);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByIdAsync(documentId, tenantId), Times.Once);
    }

    [Fact]
    public async Task GetTenantDocumentsAsync_ReturnsDocuments()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var documents = new List<Document>
        {
            new() { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Doc1", ContentType = "application/pdf", Size = 100, BlobPath = "/path1", UploadedBy = "user1" },
            new() { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Doc2", ContentType = "text/plain", Size = 200, BlobPath = "/path2", UploadedBy = "user2" }
        };

        _mockRepository.Setup(r => r.GetByTenantAsync(tenantId, false))
            .ReturnsAsync(documents);

        // Act
        var result = await _service.GetTenantDocumentsAsync(tenantId);

        // Assert
        var resultList = result.ToList();
        Assert.Equal(2, resultList.Count);
        Assert.Equal("Doc1", resultList[0].Name);
        Assert.Equal("Doc2", resultList[1].Name);
        _mockRepository.Verify(r => r.GetByTenantAsync(tenantId, false), Times.Once);
    }

    [Fact]
    public async Task CreateDocumentAsync_CreatesDocument()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = "user123";
        var createDto = new CreateDocumentDto
        {
            Name = "New Document",
            ContentType = "application/pdf",
            Size = 2048,
            BlobPath = "/path/to/new/blob"
        };

        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<Document>()))
            .ReturnsAsync((Document doc) =>
            {
                doc.Id = Guid.NewGuid();
                return doc;
            });

        // Act
        var result = await _service.CreateDocumentAsync(createDto, tenantId, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Document", result.Name);
        Assert.Equal(tenantId, result.TenantId);
        Assert.Equal(userId, result.UploadedBy);
        Assert.Equal(createDto.Size, result.Size);
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<Document>()), Times.Once);
    }

    [Fact]
    public async Task UpdateDocumentAsync_WithValidId_UpdatesDocument()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var existingDocument = new Document
        {
            Id = documentId,
            TenantId = tenantId,
            Name = "Old Name",
            ContentType = "application/pdf",
            Size = 1024,
            BlobPath = "/path",
            UploadedBy = "user"
        };

        var updateDto = new UpdateDocumentDto { Name = "New Name" };

        _mockRepository.Setup(r => r.GetByIdAsync(documentId, tenantId))
            .ReturnsAsync(existingDocument);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Document>()))
            .ReturnsAsync((Document doc) => doc);

        // Act
        var result = await _service.UpdateDocumentAsync(documentId, updateDto, tenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Name", result.Name);
        _mockRepository.Verify(r => r.GetByIdAsync(documentId, tenantId), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Document>()), Times.Once);
    }

    [Fact]
    public async Task UpdateDocumentAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var updateDto = new UpdateDocumentDto { Name = "New Name" };

        _mockRepository.Setup(r => r.GetByIdAsync(documentId, tenantId))
            .ReturnsAsync((Document?)null);

        // Act
        var result = await _service.UpdateDocumentAsync(documentId, updateDto, tenantId);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByIdAsync(documentId, tenantId), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Document>()), Times.Never);
    }

    [Fact]
    public async Task DeleteDocumentAsync_WithValidId_ReturnsTrue()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var documentId = Guid.NewGuid();

        _mockRepository.Setup(r => r.DeleteAsync(documentId, tenantId))
            .ReturnsAsync(true);

        // Act
        var result = await _service.DeleteDocumentAsync(documentId, tenantId);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.DeleteAsync(documentId, tenantId), Times.Once);
    }

    [Fact]
    public async Task DeleteDocumentAsync_WithInvalidId_ReturnsFalse()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var documentId = Guid.NewGuid();

        _mockRepository.Setup(r => r.DeleteAsync(documentId, tenantId))
            .ReturnsAsync(false);

        // Act
        var result = await _service.DeleteDocumentAsync(documentId, tenantId);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.DeleteAsync(documentId, tenantId), Times.Once);
    }
}
