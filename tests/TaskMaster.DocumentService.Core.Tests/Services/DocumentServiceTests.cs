using Microsoft.Extensions.Logging;
using Moq;
using TaskMaster.DocumentService.Core.DTOs;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Interfaces;
using DocumentServiceClass = TaskMaster.DocumentService.Core.Services.DocumentService;

namespace TaskMaster.DocumentService.Core.Tests.Services;

/// <summary>
/// Unit tests for DocumentService
/// </summary>
public class DocumentServiceTests
{
    private readonly Mock<IDocumentRepository> _mockRepository;
    private readonly Mock<ILogger<DocumentServiceClass>> _mockLogger;
    private readonly DocumentServiceClass _service;

    public DocumentServiceTests()
    {
        _mockRepository = new Mock<IDocumentRepository>();
        _mockLogger = new Mock<ILogger<DocumentServiceClass>>();
        _service = new DocumentServiceClass(_mockRepository.Object, _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new DocumentServiceClass(null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new DocumentServiceClass(_mockRepository.Object, null!));
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingDocument_ReturnsDocumentDto()
    {
        // Arrange
        var documentId = 1L;
        var document = CreateSampleDocument(documentId);

        _mockRepository
            .Setup(r => r.GetByIdAsync(documentId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        // Act
        var result = await _service.GetByIdAsync(documentId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(documentId, result.Id);
        Assert.Equal(document.Title, result.Title);
        Assert.Equal(document.TenantId, result.TenantId);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingDocument_ReturnsNull()
    {
        // Arrange
        var documentId = 999L;

        _mockRepository
            .Setup(r => r.GetByIdAsync(documentId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);

        // Act
        var result = await _service.GetByIdAsync(documentId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetByTenantIdAsync Tests

    [Fact]
    public async Task GetByTenantIdAsync_WithDocuments_ReturnsDocumentList()
    {
        // Arrange
        var tenantId = 1;
        var documents = new List<Document>
        {
            CreateSampleDocument(1L, tenantId),
            CreateSampleDocument(2L, tenantId)
        };

        _mockRepository
            .Setup(r => r.GetByTenantIdAsync(tenantId, 0, 50, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        // Act
        var result = await _service.GetByTenantIdAsync(tenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetByTenantIdAsync_WithNoDocuments_ReturnsEmptyList()
    {
        // Arrange
        var tenantId = 1;

        _mockRepository
            .Setup(r => r.GetByTenantIdAsync(tenantId, 0, 50, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Document>());

        // Act
        var result = await _service.GetByTenantIdAsync(tenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidData_ReturnsCreatedDocument()
    {
        // Arrange
        var createDto = new CreateDocumentDto
        {
            TenantId = 1,
            DocumentTypeId = 1,
            Title = "Test Document",
            Description = "Test Description",
            BlobPath = "/blobs/test.pdf",
            ContentHash = "abc123",
            FileSizeBytes = 1024,
            MimeType = "application/pdf",
            OriginalFileName = "test.pdf",
            CreatedBy = "testuser"
        };

        var createdDocument = CreateSampleDocument(1L);
        _mockRepository
            .Setup(r => r.ExistsByContentHashAsync(createDto.TenantId, createDto.ContentHash!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockRepository
            .Setup(r => r.CreateAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDocument);

        // Act
        var result = await _service.CreateAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(createdDocument.Id, result.Id);
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateContentHash_ThrowsInvalidOperationException()
    {
        // Arrange
        var createDto = new CreateDocumentDto
        {
            TenantId = 1,
            DocumentTypeId = 1,
            Title = "Test Document",
            BlobPath = "/blobs/test.pdf",
            ContentHash = "abc123"
        };

        _mockRepository
            .Setup(r => r.ExistsByContentHashAsync(createDto.TenantId, createDto.ContentHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateAsync(createDto));

        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithNullDto_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.CreateAsync(null!));
    }

    #endregion

    #region CreateVersionAsync Tests

    [Fact]
    public async Task CreateVersionAsync_WithValidDocument_CreatesNewVersion()
    {
        // Arrange
        var documentId = 1L;
        var originalDocument = CreateSampleDocument(documentId);
        var versionDto = new CreateDocumentVersionDto
        {
            BlobPath = "/blobs/test-v2.pdf",
            ContentHash = "def456",
            FileSizeBytes = 2048,
            MimeType = "application/pdf",
            OriginalFileName = "test-v2.pdf",
            CreatedBy = "testuser"
        };

        var newVersion = CreateSampleDocument(2L);
        newVersion.Version = 2;
        newVersion.ParentDocumentId = documentId;

        _mockRepository
            .Setup(r => r.GetByIdAsync(documentId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(originalDocument);

        _mockRepository
            .Setup(r => r.GetCurrentVersionAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(originalDocument);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(originalDocument);

        _mockRepository
            .Setup(r => r.CreateAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newVersion);

        // Act
        var result = await _service.CreateVersionAsync(documentId, versionDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Version);
        Assert.Equal(documentId, result.ParentDocumentId);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateVersionAsync_WithNonExistingDocument_ThrowsInvalidOperationException()
    {
        // Arrange
        var documentId = 999L;
        var versionDto = new CreateDocumentVersionDto
        {
            BlobPath = "/blobs/test.pdf"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(documentId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateVersionAsync(documentId, versionDto));
    }

    [Fact]
    public async Task CreateVersionAsync_WithNullDto_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.CreateVersionAsync(1L, null!));
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidData_ReturnsUpdatedDocument()
    {
        // Arrange
        var documentId = 1L;
        var document = CreateSampleDocument(documentId);
        var updateDto = new UpdateDocumentDto
        {
            Title = "Updated Title",
            Description = "Updated Description",
            UpdatedBy = "testuser"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(documentId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        // Act
        var result = await _service.UpdateAsync(documentId, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(documentId, result.Id);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistingDocument_ReturnsNull()
    {
        // Arrange
        var documentId = 999L;
        var updateDto = new UpdateDocumentDto
        {
            Title = "Updated Title"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(documentId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);

        // Act
        var result = await _service.UpdateAsync(documentId, updateDto);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithNullDto_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.UpdateAsync(1L, null!));
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithExistingDocument_ReturnsTrue()
    {
        // Arrange
        var documentId = 1L;

        _mockRepository
            .Setup(r => r.SoftDeleteAsync(documentId, "testuser", "Test reason", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.DeleteAsync(documentId, "testuser", "Test reason");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingDocument_ReturnsFalse()
    {
        // Arrange
        var documentId = 999L;

        _mockRepository
            .Setup(r => r.SoftDeleteAsync(documentId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.DeleteAsync(documentId, null, null);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region ArchiveAsync Tests

    [Fact]
    public async Task ArchiveAsync_WithExistingDocument_ReturnsTrue()
    {
        // Arrange
        var documentId = 1L;

        _mockRepository
            .Setup(r => r.ArchiveAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.ArchiveAsync(documentId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ArchiveAsync_WithNonExistingDocument_ReturnsFalse()
    {
        // Arrange
        var documentId = 999L;

        _mockRepository
            .Setup(r => r.ArchiveAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.ArchiveAsync(documentId);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region UnarchiveAsync Tests

    [Fact]
    public async Task UnarchiveAsync_WithExistingDocument_ReturnsTrue()
    {
        // Arrange
        var documentId = 1L;

        _mockRepository
            .Setup(r => r.UnarchiveAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.UnarchiveAsync(documentId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task UnarchiveAsync_WithNonExistingDocument_ReturnsFalse()
    {
        // Arrange
        var documentId = 999L;

        _mockRepository
            .Setup(r => r.UnarchiveAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.UnarchiveAsync(documentId);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetCountByTenantIdAsync Tests

    [Fact]
    public async Task GetCountByTenantIdAsync_ReturnsCorrectCount()
    {
        // Arrange
        var tenantId = 1;
        var expectedCount = 5;

        _mockRepository
            .Setup(r => r.GetCountByTenantIdAsync(tenantId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCount);

        // Act
        var result = await _service.GetCountByTenantIdAsync(tenantId);

        // Assert
        Assert.Equal(expectedCount, result);
    }

    #endregion

    #region GetVersionsAsync Tests

    [Fact]
    public async Task GetVersionsAsync_ReturnsAllVersions()
    {
        // Arrange
        var documentId = 1L;
        var doc1 = CreateSampleDocument(1L);
        doc1.Version = 1;

        var doc2 = CreateSampleDocument(2L);
        doc2.Version = 2;
        doc2.ParentDocumentId = 1L;

        var doc3 = CreateSampleDocument(3L);
        doc3.Version = 3;
        doc3.ParentDocumentId = 1L;

        var versions = new List<Document> { doc1, doc2, doc3 };

        _mockRepository
            .Setup(r => r.GetVersionsAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(versions);

        // Act
        var result = await _service.GetVersionsAsync(documentId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
    }

    #endregion

    #region Helper Methods

    private static Document CreateSampleDocument(long id, int tenantId = 1)
    {
        return new Document
        {
            Id = id,
            TenantId = tenantId,
            DocumentTypeId = 1,
            Title = "Test Document",
            Description = "Test Description",
            BlobPath = "/blobs/test.pdf",
            ContentHash = "abc123",
            FileSizeBytes = 1024,
            MimeType = "application/pdf",
            OriginalFileName = "test.pdf",
            Version = 1,
            IsCurrentVersion = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "testuser",
            IsDeleted = false,
            IsArchived = false
        };
    }

    #endregion
}
