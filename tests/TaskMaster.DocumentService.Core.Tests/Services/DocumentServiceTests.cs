using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Interfaces;
using TaskMaster.DocumentService.Core.Services;
using DocumentServiceClass = TaskMaster.DocumentService.Core.Services.DocumentService;

namespace TaskMaster.DocumentService.Core.Tests.Services;

/// <summary>
/// Unit tests for DocumentService.
/// </summary>
public class DocumentServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IBlobStorageService> _mockBlobStorageService;
    private readonly Mock<ILogger<DocumentServiceClass>> _mockLogger;
    private readonly Mock<IOptions<BlobStorageOptions>> _mockOptions;
    private readonly Mock<IDocumentRepository> _mockDocumentRepository;
    private readonly Mock<ITenantRepository> _mockTenantRepository;
    private readonly Mock<IDocumentTypeRepository> _mockDocumentTypeRepository;
    private readonly BlobStorageOptions _options;
    private readonly DocumentServiceClass _documentService;

    public DocumentServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockBlobStorageService = new Mock<IBlobStorageService>();
        _mockLogger = new Mock<ILogger<DocumentServiceClass>>();
        _mockDocumentRepository = new Mock<IDocumentRepository>();
        _mockTenantRepository = new Mock<ITenantRepository>();
        _mockDocumentTypeRepository = new Mock<IDocumentTypeRepository>();

        _options = new BlobStorageOptions
        {
            ConnectionString = "UseDevelopmentStorage=true",
            DefaultContainerName = "documents"
        };
        _mockOptions = new Mock<IOptions<BlobStorageOptions>>();
        _mockOptions.Setup(x => x.Value).Returns(_options);

        _mockUnitOfWork.Setup(x => x.Documents).Returns(_mockDocumentRepository.Object);
        _mockUnitOfWork.Setup(x => x.Tenants).Returns(_mockTenantRepository.Object);
        _mockUnitOfWork.Setup(x => x.DocumentTypes).Returns(_mockDocumentTypeRepository.Object);

        _documentService = new DocumentServiceClass(
            _mockUnitOfWork.Object,
            _mockBlobStorageService.Object,
            _mockLogger.Object,
            _mockOptions.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange & Act
        var service = new DocumentServiceClass(
            _mockUnitOfWork.Object,
            _mockBlobStorageService.Object,
            _mockLogger.Object,
            _mockOptions.Object);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithNullUnitOfWork_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new DocumentServiceClass(null!, _mockBlobStorageService.Object, _mockLogger.Object, _mockOptions.Object));

        Assert.Equal("unitOfWork", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullBlobStorageService_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new DocumentServiceClass(_mockUnitOfWork.Object, null!, _mockLogger.Object, _mockOptions.Object));

        Assert.Equal("blobStorageService", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new DocumentServiceClass(_mockUnitOfWork.Object, _mockBlobStorageService.Object, null!, _mockOptions.Object));

        Assert.Equal("logger", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new DocumentServiceClass(_mockUnitOfWork.Object, _mockBlobStorageService.Object, _mockLogger.Object, null!));

        Assert.Equal("blobStorageOptions", exception.ParamName);
    }

    #endregion

    #region CreateDocumentAsync Tests

    [Fact]
    public async Task CreateDocumentAsync_WithValidParameters_ShouldCreateDocument()
    {
        // Arrange
        var tenantId = 1;
        var documentTypeId = 1;
        var title = "Test Document";
        var description = "Test Description";
        var fileName = "test.txt";
        var contentType = "text/plain";
        var createdBy = "testuser";
        var content = new MemoryStream(Encoding.UTF8.GetBytes("test content"));
        var blobUri = "https://test.blob.core.windows.net/documents/test-blob";

        var tenant = new Tenant { Id = tenantId, Name = "Test Tenant" };
        var documentType = new DocumentType { Id = documentTypeId, Name = "Test Type" };

        _mockTenantRepository.Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        _mockDocumentTypeRepository.Setup(x => x.GetByIdAsync(documentTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documentType);
        _mockBlobStorageService.Setup(x => x.UploadAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(blobUri);
        _mockDocumentRepository.Setup(x => x.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document doc, CancellationToken ct) => { doc.Id = 1; return doc; });

        // Act
        var result = await _documentService.CreateDocumentAsync(
            tenantId, documentTypeId, title, description, content, fileName,
            contentType, null, null, createdBy);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tenantId, result.TenantId);
        Assert.Equal(documentTypeId, result.DocumentTypeId);
        Assert.Equal(title, result.Title);
        Assert.Equal(description, result.Description);
        Assert.Equal(fileName, result.OriginalFileName);
        Assert.Equal(contentType, result.MimeType);
        Assert.Equal(createdBy, result.CreatedBy);
        Assert.Equal(1, result.Version);
        Assert.True(result.IsCurrentVersion);
        Assert.False(result.IsDeleted);
        Assert.False(result.IsArchived);

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateDocumentAsync_WithNullTitle_ShouldThrowArgumentException()
    {
        // Arrange
        var content = new MemoryStream();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _documentService.CreateDocumentAsync(1, 1, null!, "desc", content, "file.txt", "text/plain", null, null, "user"));

        Assert.Equal("title", exception.ParamName);
    }

    [Fact]
    public async Task CreateDocumentAsync_WithNullContent_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _documentService.CreateDocumentAsync(1, 1, "title", "desc", null!, "file.txt", "text/plain", null, null, "user"));

        Assert.Equal("content", exception.ParamName);
    }

    [Fact]
    public async Task CreateDocumentAsync_WithNullFileName_ShouldThrowArgumentException()
    {
        // Arrange
        var content = new MemoryStream();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _documentService.CreateDocumentAsync(1, 1, "title", "desc", content, null!, "text/plain", null, null, "user"));

        Assert.Equal("fileName", exception.ParamName);
    }

    [Fact]
    public async Task CreateDocumentAsync_WithNonExistentTenant_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var content = new MemoryStream();
        _mockTenantRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _documentService.CreateDocumentAsync(1, 1, "title", "desc", content, "file.txt", "text/plain", null, null, "user"));

        Assert.Contains("Tenant", exception.Message);
    }

    [Fact]
    public async Task CreateDocumentAsync_WithNonExistentDocumentType_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var content = new MemoryStream();
        var tenant = new Tenant { Id = 1, Name = "Test Tenant" };

        _mockTenantRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        _mockDocumentTypeRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentType?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _documentService.CreateDocumentAsync(1, 1, "title", "desc", content, "file.txt", "text/plain", null, null, "user"));

        Assert.Contains("Document type", exception.Message);
    }

    #endregion

    #region GetDocumentByIdAsync Tests

    [Fact]
    public async Task GetDocumentByIdAsync_WithExistingDocument_ShouldReturnDocument()
    {
        // Arrange
        var documentId = 1L;
        var document = new Document { Id = documentId, Title = "Test Document" };

        _mockDocumentRepository.Setup(x => x.GetByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        // Act
        var result = await _documentService.GetDocumentByIdAsync(documentId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(documentId, result.Id);
        Assert.Equal("Test Document", result.Title);
    }

    [Fact]
    public async Task GetDocumentByIdAsync_WithNonExistentDocument_ShouldReturnNull()
    {
        // Arrange
        var documentId = 999L;
        _mockDocumentRepository.Setup(x => x.GetByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);

        // Act
        var result = await _documentService.GetDocumentByIdAsync(documentId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetDocumentsByTenantAsync Tests

    [Fact]
    public async Task GetDocumentsByTenantAsync_ShouldReturnDocuments()
    {
        // Arrange
        var tenantId = 1;
        var documents = new List<Document>
        {
            new Document { Id = 1, TenantId = tenantId, Title = "Doc 1" },
            new Document { Id = 2, TenantId = tenantId, Title = "Doc 2" }
        };

        _mockDocumentRepository.Setup(x => x.GetByTenantIdAsync(tenantId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        // Act
        var result = await _documentService.GetDocumentsByTenantAsync(tenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    #endregion

    #region GetDocumentsByTypeAsync Tests

    [Fact]
    public async Task GetDocumentsByTypeAsync_ShouldReturnDocuments()
    {
        // Arrange
        var documentTypeId = 1;
        var documents = new List<Document>
        {
            new Document { Id = 1, DocumentTypeId = documentTypeId, Title = "Doc 1" },
            new Document { Id = 2, DocumentTypeId = documentTypeId, Title = "Doc 2" }
        };

        _mockDocumentRepository.Setup(x => x.GetByDocumentTypeIdAsync(documentTypeId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        // Act
        var result = await _documentService.GetDocumentsByTypeAsync(documentTypeId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    #endregion

    #region UpdateDocumentMetadataAsync Tests

    [Fact]
    public async Task UpdateDocumentMetadataAsync_WithValidParameters_ShouldUpdateDocument()
    {
        // Arrange
        var documentId = 1L;
        var document = new Document
        {
            Id = documentId,
            Title = "Old Title",
            Description = "Old Description",
            IsDeleted = false
        };
        var newTitle = "New Title";
        var newDescription = "New Description";
        var updatedBy = "testuser";

        _mockDocumentRepository.Setup(x => x.GetByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        // Act
        var result = await _documentService.UpdateDocumentMetadataAsync(
            documentId, newTitle, newDescription, null, null, updatedBy);

        // Assert
        Assert.Equal(newTitle, result.Title);
        Assert.Equal(newDescription, result.Description);
        Assert.Equal(updatedBy, result.UpdatedBy);
        Assert.NotNull(result.UpdatedAt);

        _mockDocumentRepository.Verify(x => x.Update(It.IsAny<Document>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateDocumentMetadataAsync_WithNonExistentDocument_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockDocumentRepository.Setup(x => x.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _documentService.UpdateDocumentMetadataAsync(1L, "title", null, null, null, "user"));

        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task UpdateDocumentMetadataAsync_WithDeletedDocument_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var document = new Document { Id = 1L, IsDeleted = true };
        _mockDocumentRepository.Setup(x => x.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _documentService.UpdateDocumentMetadataAsync(1L, "title", null, null, null, "user"));

        Assert.Contains("deleted", exception.Message);
    }

    #endregion

    #region CreateDocumentVersionAsync Tests

    [Fact]
    public async Task CreateDocumentVersionAsync_WithValidParameters_ShouldCreateNewVersion()
    {
        // Arrange
        var parentDocumentId = 1L;
        var parentDocument = new Document
        {
            Id = parentDocumentId,
            TenantId = 1,
            DocumentTypeId = 1,
            Title = "Test Document",
            Version = 1,
            IsCurrentVersion = true,
            IsDeleted = false,
            ContentHash = "oldhash"
        };
        var fileName = "test-v2.txt";
        var contentType = "text/plain";
        var updatedBy = "testuser";
        var content = new MemoryStream(Encoding.UTF8.GetBytes("new content"));
        var blobUri = "https://test.blob.core.windows.net/documents/test-blob-v2";

        _mockDocumentRepository.Setup(x => x.GetByIdAsync(parentDocumentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentDocument);
        _mockDocumentRepository.Setup(x => x.GetVersionsAsync(parentDocumentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Document>());
        _mockDocumentRepository.Setup(x => x.GetCurrentVersionAsync(parentDocumentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);
        _mockBlobStorageService.Setup(x => x.UploadAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(blobUri);
        _mockDocumentRepository.Setup(x => x.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document doc, CancellationToken ct) => { doc.Id = 2; return doc; });

        // Act
        var result = await _documentService.CreateDocumentVersionAsync(
            parentDocumentId, content, fileName, contentType, updatedBy);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Version);
        Assert.Equal(parentDocumentId, result.ParentDocumentId);
        Assert.True(result.IsCurrentVersion);
        Assert.Equal(updatedBy, result.CreatedBy);

        _mockUnitOfWork.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateDocumentVersionAsync_WithSameContent_ShouldReturnParentDocument()
    {
        // Arrange
        var parentDocumentId = 1L;
        // Compute actual hash for "test" content
        var testContent = Encoding.UTF8.GetBytes("test");
        var contentHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(testContent)).ToLowerInvariant();
        var parentDocument = new Document
        {
            Id = parentDocumentId,
            ContentHash = contentHash,
            IsDeleted = false
        };
        var content = new MemoryStream(testContent);

        _mockDocumentRepository.Setup(x => x.GetByIdAsync(parentDocumentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentDocument);

        // Act
        var result = await _documentService.CreateDocumentVersionAsync(
            parentDocumentId, content, "file.txt", "text/plain", "user");

        // Assert
        Assert.Equal(parentDocument.Id, result.Id);
        _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockDocumentRepository.Verify(x => x.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateDocumentVersionAsync_WithDeletedParent_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var parentDocument = new Document { Id = 1L, IsDeleted = true };
        var content = new MemoryStream();

        _mockDocumentRepository.Setup(x => x.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentDocument);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _documentService.CreateDocumentVersionAsync(1L, content, "file.txt", "text/plain", "user"));

        Assert.Contains("deleted", exception.Message);
    }

    #endregion

    #region GetDocumentVersionsAsync Tests

    [Fact]
    public async Task GetDocumentVersionsAsync_ShouldReturnVersions()
    {
        // Arrange
        var parentDocumentId = 1L;
        var versions = new List<Document>
        {
            new Document { Id = 2, ParentDocumentId = parentDocumentId, Version = 2 },
            new Document { Id = 1, ParentDocumentId = parentDocumentId, Version = 1 }
        };

        _mockDocumentRepository.Setup(x => x.GetVersionsAsync(parentDocumentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(versions);

        // Act
        var result = await _documentService.GetDocumentVersionsAsync(parentDocumentId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    #endregion

    #region GetCurrentVersionAsync Tests

    [Fact]
    public async Task GetCurrentVersionAsync_ShouldReturnCurrentVersion()
    {
        // Arrange
        var parentDocumentId = 1L;
        var currentVersion = new Document
        {
            Id = 2,
            ParentDocumentId = parentDocumentId,
            Version = 2,
            IsCurrentVersion = true
        };

        _mockDocumentRepository.Setup(x => x.GetCurrentVersionAsync(parentDocumentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentVersion);

        // Act
        var result = await _documentService.GetCurrentVersionAsync(parentDocumentId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Version);
        Assert.True(result.IsCurrentVersion);
    }

    #endregion

    #region DownloadDocumentAsync Tests

    [Fact]
    public async Task DownloadDocumentAsync_WithValidDocument_ShouldReturnStream()
    {
        // Arrange
        var documentId = 1L;
        var document = new Document
        {
            Id = documentId,
            BlobPath = "test/blob.txt",
            IsDeleted = false
        };
        var expectedStream = new MemoryStream();

        _mockDocumentRepository.Setup(x => x.GetByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);
        _mockBlobStorageService.Setup(x => x.DownloadAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStream);

        // Act
        var result = await _documentService.DownloadDocumentAsync(documentId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedStream, result);
    }

    [Fact]
    public async Task DownloadDocumentAsync_WithDeletedDocument_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var document = new Document { Id = 1L, IsDeleted = true };
        _mockDocumentRepository.Setup(x => x.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _documentService.DownloadDocumentAsync(1L));

        Assert.Contains("deleted", exception.Message);
    }

    #endregion

    #region GetDocumentSasUriAsync Tests

    [Fact]
    public async Task GetDocumentSasUriAsync_WithValidDocument_ShouldReturnSasUri()
    {
        // Arrange
        var documentId = 1L;
        var document = new Document
        {
            Id = documentId,
            BlobPath = "test/blob.txt",
            IsDeleted = false
        };
        var expectedSasUri = "https://test.blob.core.windows.net/documents/test-blob?sas=token";
        var expiresIn = TimeSpan.FromHours(1);

        _mockDocumentRepository.Setup(x => x.GetByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);
        _mockBlobStorageService.Setup(x => x.GetSasUriAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSasUri);

        // Act
        var result = await _documentService.GetDocumentSasUriAsync(documentId, expiresIn);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedSasUri, result);
    }

    #endregion

    #region DeleteDocumentAsync Tests

    [Fact]
    public async Task DeleteDocumentAsync_WithValidDocument_ShouldSoftDelete()
    {
        // Arrange
        var documentId = 1L;
        var deletedBy = "testuser";
        var deletedReason = "Test reason";

        // Act
        await _documentService.DeleteDocumentAsync(documentId, deletedBy, deletedReason);

        // Assert
        _mockDocumentRepository.Verify(x => x.SoftDeleteAsync(
            documentId, deletedBy, deletedReason, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteDocumentAsync_WithNullDeletedBy_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _documentService.DeleteDocumentAsync(1L, null!, null));

        Assert.Equal("deletedBy", exception.ParamName);
    }

    #endregion

    #region RestoreDocumentAsync Tests

    [Fact]
    public async Task RestoreDocumentAsync_ShouldCallRepositoryRestore()
    {
        // Arrange
        var documentId = 1L;

        // Act
        await _documentService.RestoreDocumentAsync(documentId);

        // Assert
        _mockDocumentRepository.Verify(x => x.RestoreAsync(documentId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region ArchiveDocumentAsync Tests

    [Fact]
    public async Task ArchiveDocumentAsync_ShouldCallRepositoryArchive()
    {
        // Arrange
        var documentId = 1L;

        // Act
        await _documentService.ArchiveDocumentAsync(documentId);

        // Assert
        _mockDocumentRepository.Verify(x => x.ArchiveAsync(documentId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetArchivedDocumentsAsync Tests

    [Fact]
    public async Task GetArchivedDocumentsAsync_ShouldReturnArchivedDocuments()
    {
        // Arrange
        var tenantId = 1;
        var documents = new List<Document>
        {
            new Document { Id = 1, TenantId = tenantId, IsArchived = true },
            new Document { Id = 2, TenantId = tenantId, IsArchived = true }
        };

        _mockDocumentRepository.Setup(x => x.GetArchivedDocumentsAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        // Act
        var result = await _documentService.GetArchivedDocumentsAsync(tenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    #endregion

    #region PermanentlyDeleteDocumentAsync Tests

    [Fact]
    public async Task PermanentlyDeleteDocumentAsync_WithValidDocument_ShouldDeleteFromBlobAndDatabase()
    {
        // Arrange
        var documentId = 1L;
        var document = new Document
        {
            Id = documentId,
            BlobPath = "test/blob.txt"
        };

        _mockDocumentRepository.Setup(x => x.GetByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);
        _mockBlobStorageService.Setup(x => x.DeleteAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _documentService.PermanentlyDeleteDocumentAsync(documentId);

        // Assert
        _mockBlobStorageService.Verify(x => x.DeleteAsync(
            _options.DefaultContainerName, document.BlobPath, It.IsAny<CancellationToken>()), Times.Once);
        _mockDocumentRepository.Verify(x => x.Remove(document), Times.Once);
        _mockUnitOfWork.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PermanentlyDeleteDocumentAsync_WithNonExistentDocument_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockDocumentRepository.Setup(x => x.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _documentService.PermanentlyDeleteDocumentAsync(1L));

        Assert.Contains("not found", exception.Message);
    }

    #endregion

    #region FindDuplicateDocumentsAsync Tests

    [Fact]
    public async Task FindDuplicateDocumentsAsync_WithValidHash_ShouldReturnDuplicates()
    {
        // Arrange
        var contentHash = "testhash123";
        var documents = new List<Document>
        {
            new Document { Id = 1, ContentHash = contentHash },
            new Document { Id = 2, ContentHash = contentHash }
        };

        _mockDocumentRepository.Setup(x => x.GetByContentHashAsync(contentHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        // Act
        var result = await _documentService.FindDuplicateDocumentsAsync(contentHash);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task FindDuplicateDocumentsAsync_WithNullHash_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _documentService.FindDuplicateDocumentsAsync(null!));

        Assert.Equal("contentHash", exception.ParamName);
    }

    #endregion
}
