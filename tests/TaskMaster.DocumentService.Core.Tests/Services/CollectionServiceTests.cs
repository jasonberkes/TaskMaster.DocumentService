using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TaskMaster.DocumentService.Core.DTOs;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Interfaces;
using TaskMaster.DocumentService.Core.Services;

namespace TaskMaster.DocumentService.Core.Tests.Services;

/// <summary>
/// Unit tests for the CollectionService class.
/// </summary>
public class CollectionServiceTests
{
    private readonly Mock<ICollectionRepository> _mockRepository;
    private readonly Mock<ILogger<CollectionService>> _mockLogger;
    private readonly CollectionService _service;

    public CollectionServiceTests()
    {
        _mockRepository = new Mock<ICollectionRepository>();
        _mockLogger = new Mock<ILogger<CollectionService>>();
        _service = new CollectionService(_mockRepository.Object, _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenRepositoryIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CollectionService(null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CollectionService(_mockRepository.Object, null!));
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ShouldReturnCollection_WhenCollectionExists()
    {
        // Arrange
        var collectionId = 1L;
        var collection = CreateTestCollection(collectionId);
        _mockRepository.Setup(r => r.GetByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(collection);

        // Act
        var result = await _service.GetByIdAsync(collectionId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(collectionId);
        result.Name.Should().Be("Test Collection");
        _mockRepository.Verify(r => r.GetByIdAsync(collectionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenCollectionDoesNotExist()
    {
        // Arrange
        var collectionId = 999L;
        _mockRepository.Setup(r => r.GetByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Collection?)null);

        // Act
        var result = await _service.GetByIdAsync(collectionId);

        // Assert
        result.Should().BeNull();
        _mockRepository.Verify(r => r.GetByIdAsync(collectionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetBySlugAsync Tests

    [Fact]
    public async Task GetBySlugAsync_ShouldReturnCollection_WhenCollectionExists()
    {
        // Arrange
        var tenantId = 1;
        var slug = "test-collection";
        var collection = CreateTestCollection(1L, slug: slug, tenantId: tenantId);
        _mockRepository.Setup(r => r.GetBySlugAsync(tenantId, slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(collection);

        // Act
        var result = await _service.GetBySlugAsync(tenantId, slug);

        // Assert
        result.Should().NotBeNull();
        result!.Slug.Should().Be(slug);
        result.TenantId.Should().Be(tenantId);
        _mockRepository.Verify(r => r.GetBySlugAsync(tenantId, slug, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetBySlugAsync_ShouldReturnNull_WhenCollectionDoesNotExist()
    {
        // Arrange
        var tenantId = 1;
        var slug = "nonexistent-slug";
        _mockRepository.Setup(r => r.GetBySlugAsync(tenantId, slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Collection?)null);

        // Act
        var result = await _service.GetBySlugAsync(tenantId, slug);

        // Assert
        result.Should().BeNull();
        _mockRepository.Verify(r => r.GetBySlugAsync(tenantId, slug, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetByTenantIdAsync Tests

    [Fact]
    public async Task GetByTenantIdAsync_ShouldReturnCollections_WhenCollectionsExist()
    {
        // Arrange
        var tenantId = 1;
        var collections = new List<Collection>
        {
            CreateTestCollection(1L, tenantId: tenantId),
            CreateTestCollection(2L, tenantId: tenantId)
        };
        _mockRepository.Setup(r => r.GetByTenantIdAsync(tenantId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(collections);

        // Act
        var result = await _service.GetByTenantIdAsync(tenantId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.All(c => c.TenantId == tenantId).Should().BeTrue();
        _mockRepository.Verify(r => r.GetByTenantIdAsync(tenantId, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByTenantIdAsync_ShouldReturnEmptyList_WhenNoCollectionsExist()
    {
        // Arrange
        var tenantId = 1;
        _mockRepository.Setup(r => r.GetByTenantIdAsync(tenantId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Collection>());

        // Act
        var result = await _service.GetByTenantIdAsync(tenantId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        _mockRepository.Verify(r => r.GetByTenantIdAsync(tenantId, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetPublishedByTenantIdAsync Tests

    [Fact]
    public async Task GetPublishedByTenantIdAsync_ShouldReturnPublishedCollections_WhenPublishedCollectionsExist()
    {
        // Arrange
        var tenantId = 1;
        var collections = new List<Collection>
        {
            CreateTestCollection(1L, tenantId: tenantId, isPublished: true),
            CreateTestCollection(2L, tenantId: tenantId, isPublished: true)
        };
        _mockRepository.Setup(r => r.GetPublishedByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(collections);

        // Act
        var result = await _service.GetPublishedByTenantIdAsync(tenantId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.All(c => c.IsPublished).Should().BeTrue();
        _mockRepository.Verify(r => r.GetPublishedByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ShouldCreateCollection_WhenValidDataProvided()
    {
        // Arrange
        var createDto = new CreateCollectionDto
        {
            TenantId = 1,
            Name = "New Collection",
            Description = "Test Description",
            Slug = "new-collection",
            SortOrder = 1
        };
        var createdBy = "test-user";

        _mockRepository.Setup(r => r.GetBySlugAsync(createDto.TenantId, createDto.Slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Collection?)null);

        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<Collection>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Collection c, CancellationToken ct) => { c.Id = 1; return c; });

        // Act
        var result = await _service.CreateAsync(createDto, createdBy);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(createDto.Name);
        result.Slug.Should().Be(createDto.Slug);
        result.TenantId.Should().Be(createDto.TenantId);
        result.CreatedBy.Should().Be(createdBy);
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<Collection>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowInvalidOperationException_WhenSlugAlreadyExists()
    {
        // Arrange
        var createDto = new CreateCollectionDto
        {
            TenantId = 1,
            Name = "New Collection",
            Slug = "existing-slug"
        };
        var existingCollection = CreateTestCollection(1L, slug: createDto.Slug, tenantId: createDto.TenantId);

        _mockRepository.Setup(r => r.GetBySlugAsync(createDto.TenantId, createDto.Slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCollection);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateAsync(createDto, "test-user"));

        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<Collection>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ShouldUpdateCollection_WhenValidDataProvided()
    {
        // Arrange
        var collectionId = 1L;
        var existingCollection = CreateTestCollection(collectionId);
        var updateDto = new UpdateCollectionDto
        {
            Name = "Updated Collection",
            Description = "Updated Description",
            Slug = "updated-slug",
            Status = "Published",
            SortOrder = 2
        };
        var updatedBy = "test-user";

        _mockRepository.Setup(r => r.GetByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCollection);

        _mockRepository.Setup(r => r.GetBySlugAsync(existingCollection.TenantId, updateDto.Slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Collection?)null);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Collection>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Collection c, CancellationToken ct) => c);

        // Act
        var result = await _service.UpdateAsync(collectionId, updateDto, updatedBy);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be(updateDto.Name);
        result.Slug.Should().Be(updateDto.Slug);
        result.Status.Should().Be(updateDto.Status);
        result.UpdatedBy.Should().Be(updatedBy);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Collection>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnNull_WhenCollectionDoesNotExist()
    {
        // Arrange
        var collectionId = 999L;
        var updateDto = new UpdateCollectionDto
        {
            Name = "Updated Collection",
            Slug = "updated-slug",
            Status = "Draft"
        };

        _mockRepository.Setup(r => r.GetByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Collection?)null);

        // Act
        var result = await _service.UpdateAsync(collectionId, updateDto, "test-user");

        // Assert
        result.Should().BeNull();
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Collection>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowInvalidOperationException_WhenSlugAlreadyExistsForAnotherCollection()
    {
        // Arrange
        var collectionId = 1L;
        var existingCollection = CreateTestCollection(collectionId);
        var anotherCollection = CreateTestCollection(2L, slug: "another-slug");
        var updateDto = new UpdateCollectionDto
        {
            Name = "Updated Collection",
            Slug = "another-slug",
            Status = "Draft"
        };

        _mockRepository.Setup(r => r.GetByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCollection);

        _mockRepository.Setup(r => r.GetBySlugAsync(existingCollection.TenantId, updateDto.Slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(anotherCollection);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UpdateAsync(collectionId, updateDto, "test-user"));

        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Collection>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ShouldDeleteCollection_WhenCollectionExists()
    {
        // Arrange
        var collectionId = 1L;
        var deletedBy = "test-user";

        _mockRepository.Setup(r => r.DeleteAsync(collectionId, deletedBy, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.DeleteAsync(collectionId, deletedBy);

        // Assert
        result.Should().BeTrue();
        _mockRepository.Verify(r => r.DeleteAsync(collectionId, deletedBy, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenCollectionDoesNotExist()
    {
        // Arrange
        var collectionId = 999L;
        var deletedBy = "test-user";

        _mockRepository.Setup(r => r.DeleteAsync(collectionId, deletedBy, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.DeleteAsync(collectionId, deletedBy);

        // Assert
        result.Should().BeFalse();
        _mockRepository.Verify(r => r.DeleteAsync(collectionId, deletedBy, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region PublishAsync Tests

    [Fact]
    public async Task PublishAsync_ShouldPublishCollection_WhenCollectionExists()
    {
        // Arrange
        var collectionId = 1L;
        var publishedBy = "test-user";

        _mockRepository.Setup(r => r.PublishAsync(collectionId, publishedBy, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.PublishAsync(collectionId, publishedBy);

        // Assert
        result.Should().BeTrue();
        _mockRepository.Verify(r => r.PublishAsync(collectionId, publishedBy, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_ShouldReturnFalse_WhenCollectionDoesNotExist()
    {
        // Arrange
        var collectionId = 999L;
        var publishedBy = "test-user";

        _mockRepository.Setup(r => r.PublishAsync(collectionId, publishedBy, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.PublishAsync(collectionId, publishedBy);

        // Assert
        result.Should().BeFalse();
        _mockRepository.Verify(r => r.PublishAsync(collectionId, publishedBy, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region UnpublishAsync Tests

    [Fact]
    public async Task UnpublishAsync_ShouldUnpublishCollection_WhenCollectionExists()
    {
        // Arrange
        var collectionId = 1L;

        _mockRepository.Setup(r => r.UnpublishAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.UnpublishAsync(collectionId);

        // Assert
        result.Should().BeTrue();
        _mockRepository.Verify(r => r.UnpublishAsync(collectionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnpublishAsync_ShouldReturnFalse_WhenCollectionDoesNotExist()
    {
        // Arrange
        var collectionId = 999L;

        _mockRepository.Setup(r => r.UnpublishAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.UnpublishAsync(collectionId);

        // Assert
        result.Should().BeFalse();
        _mockRepository.Verify(r => r.UnpublishAsync(collectionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region AddDocumentAsync Tests

    [Fact]
    public async Task AddDocumentAsync_ShouldAddDocument_WhenValidDataProvided()
    {
        // Arrange
        var collectionId = 1L;
        var collection = CreateTestCollection(collectionId);
        var addDocumentDto = new AddDocumentToCollectionDto
        {
            DocumentId = 100L,
            SortOrder = 1,
            Notes = "Test notes"
        };
        var addedBy = "test-user";

        _mockRepository.Setup(r => r.GetByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(collection);

        _mockRepository.Setup(r => r.DocumentExistsInCollectionAsync(collectionId, addDocumentDto.DocumentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockRepository.Setup(r => r.AddDocumentAsync(It.IsAny<CollectionDocument>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CollectionDocument cd, CancellationToken ct) => { cd.Id = 1; return cd; });

        // Act
        var result = await _service.AddDocumentAsync(collectionId, addDocumentDto, addedBy);

        // Assert
        result.Should().NotBeNull();
        result!.DocumentId.Should().Be(addDocumentDto.DocumentId);
        result.CollectionId.Should().Be(collectionId);
        result.AddedBy.Should().Be(addedBy);
        _mockRepository.Verify(r => r.AddDocumentAsync(It.IsAny<CollectionDocument>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddDocumentAsync_ShouldReturnNull_WhenCollectionDoesNotExist()
    {
        // Arrange
        var collectionId = 999L;
        var addDocumentDto = new AddDocumentToCollectionDto { DocumentId = 100L };

        _mockRepository.Setup(r => r.GetByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Collection?)null);

        // Act
        var result = await _service.AddDocumentAsync(collectionId, addDocumentDto, "test-user");

        // Assert
        result.Should().BeNull();
        _mockRepository.Verify(r => r.AddDocumentAsync(It.IsAny<CollectionDocument>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddDocumentAsync_ShouldThrowInvalidOperationException_WhenDocumentAlreadyExists()
    {
        // Arrange
        var collectionId = 1L;
        var collection = CreateTestCollection(collectionId);
        var addDocumentDto = new AddDocumentToCollectionDto { DocumentId = 100L };

        _mockRepository.Setup(r => r.GetByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(collection);

        _mockRepository.Setup(r => r.DocumentExistsInCollectionAsync(collectionId, addDocumentDto.DocumentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.AddDocumentAsync(collectionId, addDocumentDto, "test-user"));

        _mockRepository.Verify(r => r.AddDocumentAsync(It.IsAny<CollectionDocument>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region RemoveDocumentAsync Tests

    [Fact]
    public async Task RemoveDocumentAsync_ShouldRemoveDocument_WhenDocumentExists()
    {
        // Arrange
        var collectionId = 1L;
        var documentId = 100L;

        _mockRepository.Setup(r => r.RemoveDocumentAsync(collectionId, documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.RemoveDocumentAsync(collectionId, documentId);

        // Assert
        result.Should().BeTrue();
        _mockRepository.Verify(r => r.RemoveDocumentAsync(collectionId, documentId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveDocumentAsync_ShouldReturnFalse_WhenDocumentDoesNotExist()
    {
        // Arrange
        var collectionId = 1L;
        var documentId = 999L;

        _mockRepository.Setup(r => r.RemoveDocumentAsync(collectionId, documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.RemoveDocumentAsync(collectionId, documentId);

        // Assert
        result.Should().BeFalse();
        _mockRepository.Verify(r => r.RemoveDocumentAsync(collectionId, documentId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetDocumentsAsync Tests

    [Fact]
    public async Task GetDocumentsAsync_ShouldReturnDocuments_WhenDocumentsExist()
    {
        // Arrange
        var collectionId = 1L;
        var documents = new List<CollectionDocument>
        {
            CreateTestCollectionDocument(1L, collectionId, 100L),
            CreateTestCollectionDocument(2L, collectionId, 101L)
        };

        _mockRepository.Setup(r => r.GetDocumentsAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        // Act
        var result = await _service.GetDocumentsAsync(collectionId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.All(d => d.CollectionId == collectionId).Should().BeTrue();
        _mockRepository.Verify(r => r.GetDocumentsAsync(collectionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetDocumentsAsync_ShouldReturnEmptyList_WhenNoDocumentsExist()
    {
        // Arrange
        var collectionId = 1L;

        _mockRepository.Setup(r => r.GetDocumentsAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CollectionDocument>());

        // Act
        var result = await _service.GetDocumentsAsync(collectionId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        _mockRepository.Verify(r => r.GetDocumentsAsync(collectionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Helper Methods

    private static Collection CreateTestCollection(
        long id,
        string name = "Test Collection",
        string slug = "test-collection",
        int tenantId = 1,
        bool isPublished = false)
    {
        return new Collection
        {
            Id = id,
            TenantId = tenantId,
            Name = name,
            Description = "Test Description",
            Slug = slug,
            Status = isPublished ? "Published" : "Draft",
            IsPublished = isPublished,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user",
            SortOrder = 0,
            CollectionDocuments = new List<CollectionDocument>()
        };
    }

    private static CollectionDocument CreateTestCollectionDocument(long id, long collectionId, long documentId)
    {
        return new CollectionDocument
        {
            Id = id,
            CollectionId = collectionId,
            DocumentId = documentId,
            SortOrder = 0,
            AddedAt = DateTime.UtcNow,
            AddedBy = "test-user",
            Collection = CreateTestCollection(collectionId)
        };
    }

    #endregion
}
