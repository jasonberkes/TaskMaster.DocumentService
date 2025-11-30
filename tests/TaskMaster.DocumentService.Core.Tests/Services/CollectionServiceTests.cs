using Microsoft.Extensions.Logging;
using Moq;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Interfaces;
using TaskMaster.DocumentService.Core.Services;

namespace TaskMaster.DocumentService.Core.Tests.Services;

/// <summary>
/// Unit tests for CollectionService.
/// </summary>
public class CollectionServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ICollectionRepository> _mockCollectionRepository;
    private readonly Mock<ITenantRepository> _mockTenantRepository;
    private readonly Mock<IDocumentRepository> _mockDocumentRepository;
    private readonly Mock<ILogger<CollectionService>> _mockLogger;
    private readonly CollectionService _collectionService;

    public CollectionServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockCollectionRepository = new Mock<ICollectionRepository>();
        _mockTenantRepository = new Mock<ITenantRepository>();
        _mockDocumentRepository = new Mock<IDocumentRepository>();
        _mockLogger = new Mock<ILogger<CollectionService>>();

        // Setup UnitOfWork to return the mocked repositories
        _mockUnitOfWork.Setup(x => x.Collections).Returns(_mockCollectionRepository.Object);
        _mockUnitOfWork.Setup(x => x.Tenants).Returns(_mockTenantRepository.Object);
        _mockUnitOfWork.Setup(x => x.Documents).Returns(_mockDocumentRepository.Object);

        _collectionService = new CollectionService(_mockUnitOfWork.Object, _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange & Act
        var service = new CollectionService(_mockUnitOfWork.Object, _mockLogger.Object);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithNullUnitOfWork_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new CollectionService(null!, _mockLogger.Object));

        Assert.Equal("unitOfWork", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new CollectionService(_mockUnitOfWork.Object, null!));

        Assert.Equal("logger", exception.ParamName);
    }

    #endregion

    #region CreateCollectionAsync Tests

    [Fact]
    public async Task CreateCollectionAsync_WithValidParameters_ShouldCreateCollection()
    {
        // Arrange
        var tenantId = 1;
        var name = "Test Collection";
        var slug = "test-collection";
        var description = "Test Description";
        var createdBy = "test-user";

        var tenant = new Tenant { Id = tenantId, Name = "Test Tenant", Slug = "test-tenant", TenantType = "Organization", IsActive = true };

        _mockTenantRepository
            .Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        _mockCollectionRepository
            .Setup(x => x.GetBySlugAsync(slug, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Collection?)null);

        _mockCollectionRepository
            .Setup(x => x.AddAsync(It.IsAny<Collection>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Collection c, CancellationToken ct) => c);

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _collectionService.CreateCollectionAsync(
            tenantId, name, description, slug, null, null, createdBy);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(name, result.Name);
        Assert.Equal(slug, result.Slug);
        Assert.Equal(description, result.Description);
        Assert.Equal(tenantId, result.TenantId);
        Assert.Equal(createdBy, result.CreatedBy);
        Assert.False(result.IsPublished);
        Assert.False(result.IsDeleted);

        _mockCollectionRepository.Verify(x => x.AddAsync(It.IsAny<Collection>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("", "slug", "createdBy")]
    [InlineData("  ", "slug", "createdBy")]
    public async Task CreateCollectionAsync_WithInvalidName_ShouldThrowArgumentException(
        string name, string slug, string createdBy)
    {
        // Arrange & Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _collectionService.CreateCollectionAsync(1, name, null, slug, null, null, createdBy));

        Assert.Equal("name", exception.ParamName);
    }

    [Theory]
    [InlineData("name", "", "createdBy")]
    [InlineData("name", "  ", "createdBy")]
    public async Task CreateCollectionAsync_WithInvalidSlug_ShouldThrowArgumentException(
        string name, string slug, string createdBy)
    {
        // Arrange & Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _collectionService.CreateCollectionAsync(1, name, null, slug, null, null, createdBy));

        Assert.Equal("slug", exception.ParamName);
    }

    [Fact]
    public async Task CreateCollectionAsync_WithNonExistentTenant_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var tenantId = 999;
        _mockTenantRepository
            .Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _collectionService.CreateCollectionAsync(tenantId, "name", null, "slug", null, null, "user"));

        Assert.Contains("Tenant with ID", exception.Message);
    }

    [Fact]
    public async Task CreateCollectionAsync_WithDuplicateSlug_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var tenantId = 1;
        var slug = "existing-slug";
        var existingCollection = new Collection { Id = 1, Slug = slug, TenantId = tenantId };

        var tenant = new Tenant { Id = tenantId, Name = "Test Tenant", Slug = "test-tenant", TenantType = "Organization", IsActive = true };

        _mockTenantRepository
            .Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        _mockCollectionRepository
            .Setup(x => x.GetBySlugAsync(slug, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCollection);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _collectionService.CreateCollectionAsync(tenantId, "name", null, slug, null, null, "user"));

        Assert.Contains("already exists", exception.Message);
    }

    #endregion

    #region GetCollectionByIdAsync Tests

    [Fact]
    public async Task GetCollectionByIdAsync_WithValidId_ShouldReturnCollection()
    {
        // Arrange
        var collectionId = 1L;
        var expectedCollection = new Collection
        {
            Id = collectionId,
            Name = "Test Collection",
            Slug = "test-collection",
            TenantId = 1,
            IsDeleted = false
        };

        _mockCollectionRepository
            .Setup(x => x.GetByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCollection);

        // Act
        var result = await _collectionService.GetCollectionByIdAsync(collectionId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedCollection.Id, result.Id);
        Assert.Equal(expectedCollection.Name, result.Name);
        _mockCollectionRepository.Verify(x => x.GetByIdAsync(collectionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCollectionByIdAsync_WithNonExistentId_ShouldReturnNull()
    {
        // Arrange
        var collectionId = 999L;
        _mockCollectionRepository
            .Setup(x => x.GetByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Collection?)null);

        // Act
        var result = await _collectionService.GetCollectionByIdAsync(collectionId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetCollectionBySlugAsync Tests

    [Fact]
    public async Task GetCollectionBySlugAsync_WithValidSlug_ShouldReturnCollection()
    {
        // Arrange
        var slug = "test-collection";
        var tenantId = 1;
        var expectedCollection = new Collection
        {
            Id = 1,
            Name = "Test Collection",
            Slug = slug,
            TenantId = tenantId,
            IsDeleted = false
        };

        _mockCollectionRepository
            .Setup(x => x.GetBySlugAsync(slug, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCollection);

        // Act
        var result = await _collectionService.GetCollectionBySlugAsync(slug, tenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedCollection.Slug, result.Slug);
    }

    [Fact]
    public async Task GetCollectionBySlugAsync_WithInvalidSlug_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _collectionService.GetCollectionBySlugAsync("", 1));

        Assert.Equal("slug", exception.ParamName);
    }

    #endregion

    #region UpdateCollectionAsync Tests

    [Fact]
    public async Task UpdateCollectionAsync_WithValidParameters_ShouldUpdateCollection()
    {
        // Arrange
        var collectionId = 1L;
        var updatedName = "Updated Collection";
        var updatedBy = "test-user";

        var existingCollection = new Collection
        {
            Id = collectionId,
            Name = "Original Name",
            Slug = "test-collection",
            TenantId = 1,
            IsDeleted = false
        };

        _mockCollectionRepository
            .Setup(x => x.GetByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCollection);

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _collectionService.UpdateCollectionAsync(
            collectionId, updatedName, null, null, null, null, updatedBy);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(updatedName, result.Name);
        Assert.Equal(updatedBy, result.UpdatedBy);
        Assert.NotNull(result.UpdatedAt);

        _mockCollectionRepository.Verify(x => x.Update(It.IsAny<Collection>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateCollectionAsync_WithNonExistentCollection_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var collectionId = 999L;
        _mockCollectionRepository
            .Setup(x => x.GetByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Collection?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _collectionService.UpdateCollectionAsync(collectionId, "name", null, null, null, null, "user"));

        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task UpdateCollectionAsync_WithDeletedCollection_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var collectionId = 1L;
        var deletedCollection = new Collection
        {
            Id = collectionId,
            Name = "Deleted Collection",
            Slug = "deleted",
            TenantId = 1,
            IsDeleted = true
        };

        _mockCollectionRepository
            .Setup(x => x.GetByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deletedCollection);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _collectionService.UpdateCollectionAsync(collectionId, "name", null, null, null, null, "user"));

        Assert.Contains("deleted", exception.Message);
    }

    #endregion

    #region PublishCollectionAsync Tests

    [Fact]
    public async Task PublishCollectionAsync_WithValidCollection_ShouldPublishCollection()
    {
        // Arrange
        var collectionId = 1L;
        var publishedBy = "test-user";

        var collection = new Collection
        {
            Id = collectionId,
            Name = "Test Collection",
            Slug = "test-collection",
            TenantId = 1,
            IsPublished = false,
            IsDeleted = false
        };

        _mockCollectionRepository
            .Setup(x => x.GetByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(collection);

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _collectionService.PublishCollectionAsync(collectionId, publishedBy);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsPublished);
        Assert.NotNull(result.PublishedAt);
        Assert.Equal(publishedBy, result.PublishedBy);

        _mockCollectionRepository.Verify(x => x.Update(It.IsAny<Collection>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishCollectionAsync_WithAlreadyPublishedCollection_ShouldReturnCollection()
    {
        // Arrange
        var collectionId = 1L;
        var publishedBy = "test-user";

        var collection = new Collection
        {
            Id = collectionId,
            Name = "Test Collection",
            Slug = "test-collection",
            TenantId = 1,
            IsPublished = true,
            PublishedAt = DateTime.UtcNow.AddDays(-1),
            PublishedBy = "previous-user",
            IsDeleted = false
        };

        _mockCollectionRepository
            .Setup(x => x.GetByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(collection);

        // Act
        var result = await _collectionService.PublishCollectionAsync(collectionId, publishedBy);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsPublished);
        Assert.Equal("previous-user", result.PublishedBy); // Should not change

        _mockCollectionRepository.Verify(x => x.Update(It.IsAny<Collection>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region UnpublishCollectionAsync Tests

    [Fact]
    public async Task UnpublishCollectionAsync_WithPublishedCollection_ShouldUnpublishCollection()
    {
        // Arrange
        var collectionId = 1L;

        var collection = new Collection
        {
            Id = collectionId,
            Name = "Test Collection",
            Slug = "test-collection",
            TenantId = 1,
            IsPublished = true,
            PublishedAt = DateTime.UtcNow.AddDays(-1),
            IsDeleted = false
        };

        _mockCollectionRepository
            .Setup(x => x.GetByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(collection);

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _collectionService.UnpublishCollectionAsync(collectionId);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsPublished);

        _mockCollectionRepository.Verify(x => x.Update(It.IsAny<Collection>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region AddDocumentToCollectionAsync Tests

    [Fact]
    public async Task AddDocumentToCollectionAsync_WithValidParameters_ShouldAddDocument()
    {
        // Arrange
        var collectionId = 1L;
        var documentId = 1L;
        var sortOrder = 1;
        var addedBy = "test-user";

        var collection = new Collection
        {
            Id = collectionId,
            Name = "Test Collection",
            Slug = "test",
            TenantId = 1,
            IsDeleted = false
        };

        var document = new Document
        {
            Id = documentId,
            Title = "Test Document",
            TenantId = 1,
            DocumentTypeId = 1,
            IsDeleted = false
        };

        _mockCollectionRepository
            .Setup(x => x.GetByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(collection);

        _mockDocumentRepository
            .Setup(x => x.GetByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _mockCollectionRepository
            .Setup(x => x.IsDocumentInCollectionAsync(collectionId, documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _collectionService.AddDocumentToCollectionAsync(collectionId, documentId, sortOrder, addedBy);

        // Assert
        _mockCollectionRepository.Verify(
            x => x.AddDocumentToCollectionAsync(It.IsAny<CollectionDocument>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddDocumentToCollectionAsync_WithNonExistentCollection_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var collectionId = 999L;
        _mockCollectionRepository
            .Setup(x => x.GetByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Collection?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _collectionService.AddDocumentToCollectionAsync(collectionId, 1, 1, "user"));

        Assert.Contains("Collection with ID", exception.Message);
    }

    [Fact]
    public async Task AddDocumentToCollectionAsync_WithNonExistentDocument_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var collectionId = 1L;
        var documentId = 999L;

        var collection = new Collection
        {
            Id = collectionId,
            Name = "Test Collection",
            Slug = "test",
            TenantId = 1,
            IsDeleted = false
        };

        _mockCollectionRepository
            .Setup(x => x.GetByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(collection);

        _mockDocumentRepository
            .Setup(x => x.GetByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _collectionService.AddDocumentToCollectionAsync(collectionId, documentId, 1, "user"));

        Assert.Contains("Document with ID", exception.Message);
    }

    #endregion

    #region RemoveDocumentFromCollectionAsync Tests

    [Fact]
    public async Task RemoveDocumentFromCollectionAsync_WithValidParameters_ShouldRemoveDocument()
    {
        // Arrange
        var collectionId = 1L;
        var documentId = 1L;

        var collection = new Collection
        {
            Id = collectionId,
            Name = "Test Collection",
            Slug = "test",
            TenantId = 1,
            IsDeleted = false
        };

        _mockCollectionRepository
            .Setup(x => x.GetByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(collection);

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _collectionService.RemoveDocumentFromCollectionAsync(collectionId, documentId);

        // Assert
        _mockCollectionRepository.Verify(
            x => x.RemoveDocumentFromCollectionAsync(collectionId, documentId, It.IsAny<CancellationToken>()),
            Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region DeleteCollectionAsync Tests

    [Fact]
    public async Task DeleteCollectionAsync_WithValidCollection_ShouldSoftDeleteCollection()
    {
        // Arrange
        var collectionId = 1L;
        var deletedBy = "test-user";
        var deletedReason = "Test reason";

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _collectionService.DeleteCollectionAsync(collectionId, deletedBy, deletedReason);

        // Assert
        _mockCollectionRepository.Verify(
            x => x.SoftDeleteAsync(collectionId, deletedBy, deletedReason, It.IsAny<CancellationToken>()),
            Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region RestoreCollectionAsync Tests

    [Fact]
    public async Task RestoreCollectionAsync_WithDeletedCollection_ShouldRestoreCollection()
    {
        // Arrange
        var collectionId = 1L;

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _collectionService.RestoreCollectionAsync(collectionId);

        // Assert
        _mockCollectionRepository.Verify(
            x => x.RestoreAsync(collectionId, It.IsAny<CancellationToken>()),
            Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region PermanentlyDeleteCollectionAsync Tests

    [Fact]
    public async Task PermanentlyDeleteCollectionAsync_WithValidCollection_ShouldPermanentlyDelete()
    {
        // Arrange
        var collectionId = 1L;

        var collection = new Collection
        {
            Id = collectionId,
            Name = "Test Collection",
            Slug = "test",
            TenantId = 1,
            IsDeleted = true
        };

        _mockCollectionRepository
            .Setup(x => x.GetByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(collection);

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _collectionService.PermanentlyDeleteCollectionAsync(collectionId);

        // Assert
        _mockUnitOfWork.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockCollectionRepository.Verify(x => x.Remove(It.IsAny<Collection>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
