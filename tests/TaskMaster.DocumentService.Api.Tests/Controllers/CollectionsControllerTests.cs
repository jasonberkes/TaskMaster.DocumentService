using System.Security.Claims;
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
/// Unit tests for <see cref="CollectionsController"/>.
/// </summary>
public class CollectionsControllerTests
{
    private readonly Mock<ICollectionService> _collectionService;
    private readonly Mock<ILogger<CollectionsController>> _logger;
    private readonly CollectionsController _controller;
    private readonly int _testTenantId = 1;
    private readonly string _testUserId = "testuser";

    public CollectionsControllerTests()
    {
        _collectionService = new Mock<ICollectionService>();
        _logger = new Mock<ILogger<CollectionsController>>();
        _controller = new CollectionsController(_collectionService.Object, _logger.Object);

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

    private Collection CreateTestCollection(long id = 1, bool isPublished = false, bool isDeleted = false)
    {
        return new Collection
        {
            Id = id,
            TenantId = _testTenantId,
            Name = "Test Collection",
            Description = "Test Description",
            Slug = "test-collection",
            IsPublished = isPublished,
            PublishedAt = isPublished ? DateTime.UtcNow : null,
            PublishedBy = isPublished ? _testUserId : null,
            Metadata = "{\"key\":\"value\"}",
            Tags = "[\"tag1\",\"tag2\"]",
            SortOrder = 1,
            IsDeleted = isDeleted,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _testUserId
        };
    }

    #region CreateCollection Tests

    /// <summary>
    /// Test that CreateCollection returns Created result when valid data is provided.
    /// </summary>
    [Fact]
    public async Task CreateCollection_WithValidData_ReturnsCreatedResult()
    {
        // Arrange
        var request = new CreateCollectionRequest
        {
            TenantId = _testTenantId,
            Name = "Test Collection",
            Description = "Test Description",
            Slug = "test-collection",
            Metadata = "{\"key\":\"value\"}",
            Tags = "[\"tag1\"]"
        };

        var collection = CreateTestCollection();

        _collectionService
            .Setup(x => x.CreateCollectionAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(collection);

        // Act
        var result = await _controller.CreateCollection(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.NotNull(createdResult.Value);
        Assert.Equal(nameof(CollectionsController.GetCollectionById), createdResult.ActionName);
        _collectionService.Verify(x => x.CreateCollectionAsync(
            _testTenantId, request.Name, request.Description, request.Slug,
            request.Metadata, request.Tags, _testUserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test that CreateCollection returns BadRequest when name is empty.
    /// </summary>
    [Fact]
    public async Task CreateCollection_WithEmptyName_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateCollectionRequest
        {
            TenantId = _testTenantId,
            Name = "",
            Slug = "test-collection"
        };

        // Act
        var result = await _controller.CreateCollection(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    /// <summary>
    /// Test that CreateCollection returns BadRequest when slug is empty.
    /// </summary>
    [Fact]
    public async Task CreateCollection_WithEmptySlug_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateCollectionRequest
        {
            TenantId = _testTenantId,
            Name = "Test Collection",
            Slug = ""
        };

        // Act
        var result = await _controller.CreateCollection(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test that CreateCollection handles service exceptions properly.
    /// </summary>
    [Fact]
    public async Task CreateCollection_WhenServiceThrowsInvalidOperationException_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateCollectionRequest
        {
            TenantId = _testTenantId,
            Name = "Test Collection",
            Slug = "test-collection"
        };

        _collectionService
            .Setup(x => x.CreateCollectionAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Slug already exists"));

        // Act
        var result = await _controller.CreateCollection(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    /// <summary>
    /// Test that CreateCollection returns InternalServerError for unexpected exceptions.
    /// </summary>
    [Fact]
    public async Task CreateCollection_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var request = new CreateCollectionRequest
        {
            TenantId = _testTenantId,
            Name = "Test Collection",
            Slug = "test-collection"
        };

        _collectionService
            .Setup(x => x.CreateCollectionAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _controller.CreateCollection(request);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
    }

    #endregion

    #region GetCollectionById Tests

    /// <summary>
    /// Test that GetCollectionById returns collection when it exists and belongs to user's tenant.
    /// </summary>
    [Fact]
    public async Task GetCollectionById_WithValidIdAndTenant_ReturnsOkResult()
    {
        // Arrange
        var collectionId = 1L;
        var collection = CreateTestCollection(collectionId);

        _collectionService
            .Setup(x => x.GetCollectionByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(collection);

        // Act
        var result = await _controller.GetCollectionById(collectionId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    /// <summary>
    /// Test that GetCollectionById returns NotFound when collection does not exist.
    /// </summary>
    [Fact]
    public async Task GetCollectionById_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var collectionId = 999L;
        _collectionService
            .Setup(x => x.GetCollectionByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Collection?)null);

        // Act
        var result = await _controller.GetCollectionById(collectionId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    /// <summary>
    /// Test that GetCollectionById returns NotFound when collection belongs to different tenant.
    /// </summary>
    [Fact]
    public async Task GetCollectionById_WithDifferentTenant_ReturnsNotFound()
    {
        // Arrange
        var collectionId = 1L;
        var collection = CreateTestCollection(collectionId);
        collection.TenantId = 999; // Different tenant

        _collectionService
            .Setup(x => x.GetCollectionByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(collection);

        // Act
        var result = await _controller.GetCollectionById(collectionId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    /// <summary>
    /// Test that GetCollectionById returns InternalServerError for unexpected exceptions.
    /// </summary>
    [Fact]
    public async Task GetCollectionById_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var collectionId = 1L;
        _collectionService
            .Setup(x => x.GetCollectionByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _controller.GetCollectionById(collectionId);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
    }

    #endregion

    #region GetCollectionBySlug Tests

    /// <summary>
    /// Test that GetCollectionBySlug returns collection when it exists.
    /// </summary>
    [Fact]
    public async Task GetCollectionBySlug_WithValidSlug_ReturnsOkResult()
    {
        // Arrange
        var slug = "test-collection";
        var collection = CreateTestCollection();

        _collectionService
            .Setup(x => x.GetCollectionBySlugAsync(slug, _testTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(collection);

        // Act
        var result = await _controller.GetCollectionBySlug(_testTenantId, slug);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    /// <summary>
    /// Test that GetCollectionBySlug returns NotFound when collection does not exist.
    /// </summary>
    [Fact]
    public async Task GetCollectionBySlug_WithNonExistentSlug_ReturnsNotFound()
    {
        // Arrange
        var slug = "non-existent";
        _collectionService
            .Setup(x => x.GetCollectionBySlugAsync(slug, _testTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Collection?)null);

        // Act
        var result = await _controller.GetCollectionBySlug(_testTenantId, slug);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    /// <summary>
    /// Test that GetCollectionBySlug returns BadRequest when slug is empty.
    /// </summary>
    [Fact]
    public async Task GetCollectionBySlug_WithEmptySlug_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetCollectionBySlug(_testTenantId, "");

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region GetCollectionsByTenant Tests

    /// <summary>
    /// Test that GetCollectionsByTenant returns collections for the tenant.
    /// </summary>
    [Fact]
    public async Task GetCollectionsByTenant_WithValidTenant_ReturnsOkResult()
    {
        // Arrange
        var collections = new List<Collection>
        {
            CreateTestCollection(1),
            CreateTestCollection(2)
        };

        _collectionService
            .Setup(x => x.GetCollectionsByTenantAsync(_testTenantId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(collections);

        // Act
        var result = await _controller.GetCollectionsByTenant(_testTenantId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    /// <summary>
    /// Test that GetCollectionsByTenant includes deleted when requested.
    /// </summary>
    [Fact]
    public async Task GetCollectionsByTenant_WithIncludeDeleted_ReturnsAllCollections()
    {
        // Arrange
        var collections = new List<Collection>
        {
            CreateTestCollection(1, false, false),
            CreateTestCollection(2, false, true)
        };

        _collectionService
            .Setup(x => x.GetCollectionsByTenantAsync(_testTenantId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(collections);

        // Act
        var result = await _controller.GetCollectionsByTenant(_testTenantId, includeDeleted: true);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        _collectionService.Verify(x => x.GetCollectionsByTenantAsync(_testTenantId, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetPublishedCollections Tests

    /// <summary>
    /// Test that GetPublishedCollections returns only published collections.
    /// </summary>
    [Fact]
    public async Task GetPublishedCollections_ReturnsOnlyPublishedCollections()
    {
        // Arrange
        var collections = new List<Collection>
        {
            CreateTestCollection(1, isPublished: true),
            CreateTestCollection(2, isPublished: true)
        };

        _collectionService
            .Setup(x => x.GetPublishedCollectionsAsync(_testTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(collections);

        // Act
        var result = await _controller.GetPublishedCollections(_testTenantId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region UpdateCollection Tests

    /// <summary>
    /// Test that UpdateCollection returns Ok when collection is updated successfully.
    /// </summary>
    [Fact]
    public async Task UpdateCollection_WithValidData_ReturnsOkResult()
    {
        // Arrange
        var collectionId = 1L;
        var existingCollection = CreateTestCollection(collectionId);
        var request = new UpdateCollectionRequest
        {
            Name = "Updated Name",
            Description = "Updated Description",
            Slug = "updated-slug"
        };

        _collectionService
            .Setup(x => x.GetCollectionByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCollection);

        _collectionService
            .Setup(x => x.UpdateCollectionAsync(
                collectionId, It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCollection);

        // Act
        var result = await _controller.UpdateCollection(collectionId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    /// <summary>
    /// Test that UpdateCollection returns NotFound when collection does not exist.
    /// </summary>
    [Fact]
    public async Task UpdateCollection_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var collectionId = 999L;
        var request = new UpdateCollectionRequest { Name = "Updated Name" };

        _collectionService
            .Setup(x => x.GetCollectionByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Collection?)null);

        // Act
        var result = await _controller.UpdateCollection(collectionId, request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// Test that UpdateCollection returns NotFound when collection belongs to different tenant.
    /// </summary>
    [Fact]
    public async Task UpdateCollection_WithDifferentTenant_ReturnsNotFound()
    {
        // Arrange
        var collectionId = 1L;
        var collection = CreateTestCollection(collectionId);
        collection.TenantId = 999; // Different tenant
        var request = new UpdateCollectionRequest { Name = "Updated Name" };

        _collectionService
            .Setup(x => x.GetCollectionByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(collection);

        // Act
        var result = await _controller.UpdateCollection(collectionId, request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion

    #region PublishCollection Tests

    /// <summary>
    /// Test that PublishCollection returns Ok when collection is published successfully.
    /// </summary>
    [Fact]
    public async Task PublishCollection_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var collectionId = 1L;
        var collection = CreateTestCollection(collectionId);
        var publishedCollection = CreateTestCollection(collectionId, isPublished: true);

        _collectionService
            .Setup(x => x.GetCollectionByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(collection);

        _collectionService
            .Setup(x => x.PublishCollectionAsync(collectionId, _testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(publishedCollection);

        // Act
        var result = await _controller.PublishCollection(collectionId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    /// <summary>
    /// Test that PublishCollection returns NotFound when collection does not exist.
    /// </summary>
    [Fact]
    public async Task PublishCollection_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var collectionId = 999L;
        _collectionService
            .Setup(x => x.GetCollectionByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Collection?)null);

        // Act
        var result = await _controller.PublishCollection(collectionId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion

    #region UnpublishCollection Tests

    /// <summary>
    /// Test that UnpublishCollection returns Ok when collection is unpublished successfully.
    /// </summary>
    [Fact]
    public async Task UnpublishCollection_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var collectionId = 1L;
        var collection = CreateTestCollection(collectionId, isPublished: true);
        var unpublishedCollection = CreateTestCollection(collectionId, isPublished: false);

        _collectionService
            .Setup(x => x.GetCollectionByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(collection);

        _collectionService
            .Setup(x => x.UnpublishCollectionAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(unpublishedCollection);

        // Act
        var result = await _controller.UnpublishCollection(collectionId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region GetDocumentsInCollection Tests

    /// <summary>
    /// Test that GetDocumentsInCollection returns documents successfully.
    /// </summary>
    [Fact]
    public async Task GetDocumentsInCollection_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var collectionId = 1L;
        var collection = CreateTestCollection(collectionId);
        var documents = new List<Document>
        {
            new() { Id = 1, TenantId = _testTenantId, Title = "Doc 1", CreatedAt = DateTime.UtcNow },
            new() { Id = 2, TenantId = _testTenantId, Title = "Doc 2", CreatedAt = DateTime.UtcNow }
        };

        _collectionService
            .Setup(x => x.GetCollectionByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(collection);

        _collectionService
            .Setup(x => x.GetDocumentsInCollectionAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        // Act
        var result = await _controller.GetDocumentsInCollection(collectionId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    /// <summary>
    /// Test that GetDocumentsInCollection returns NotFound when collection does not exist.
    /// </summary>
    [Fact]
    public async Task GetDocumentsInCollection_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var collectionId = 999L;
        _collectionService
            .Setup(x => x.GetCollectionByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Collection?)null);

        // Act
        var result = await _controller.GetDocumentsInCollection(collectionId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion

    #region AddDocumentToCollection Tests

    /// <summary>
    /// Test that AddDocumentToCollection returns NoContent when successful.
    /// </summary>
    [Fact]
    public async Task AddDocumentToCollection_WithValidData_ReturnsNoContent()
    {
        // Arrange
        var collectionId = 1L;
        var collection = CreateTestCollection(collectionId);
        var request = new AddDocumentToCollectionRequest
        {
            DocumentId = 1L,
            SortOrder = 1,
            Metadata = "{\"key\":\"value\"}"
        };

        _collectionService
            .Setup(x => x.GetCollectionByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(collection);

        _collectionService
            .Setup(x => x.AddDocumentToCollectionAsync(
                collectionId, request.DocumentId, request.SortOrder, _testUserId,
                request.Metadata, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.AddDocumentToCollection(collectionId, request);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    /// <summary>
    /// Test that AddDocumentToCollection returns NotFound when collection does not exist.
    /// </summary>
    [Fact]
    public async Task AddDocumentToCollection_WithNonExistentCollection_ReturnsNotFound()
    {
        // Arrange
        var collectionId = 999L;
        var request = new AddDocumentToCollectionRequest { DocumentId = 1L };

        _collectionService
            .Setup(x => x.GetCollectionByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Collection?)null);

        // Act
        var result = await _controller.AddDocumentToCollection(collectionId, request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion

    #region AddDocumentsToCollection Tests

    /// <summary>
    /// Test that AddDocumentsToCollection returns NoContent when successful.
    /// </summary>
    [Fact]
    public async Task AddDocumentsToCollection_WithValidData_ReturnsNoContent()
    {
        // Arrange
        var collectionId = 1L;
        var collection = CreateTestCollection(collectionId);
        var request = new AddDocumentsToCollectionRequest
        {
            DocumentIds = new List<long> { 1L, 2L, 3L }
        };

        _collectionService
            .Setup(x => x.GetCollectionByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(collection);

        _collectionService
            .Setup(x => x.AddDocumentsToCollectionAsync(
                collectionId, request.DocumentIds, _testUserId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.AddDocumentsToCollection(collectionId, request);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    /// <summary>
    /// Test that AddDocumentsToCollection returns BadRequest when document IDs are empty.
    /// </summary>
    [Fact]
    public async Task AddDocumentsToCollection_WithEmptyDocumentIds_ReturnsBadRequest()
    {
        // Arrange
        var collectionId = 1L;
        var request = new AddDocumentsToCollectionRequest
        {
            DocumentIds = new List<long>()
        };

        // Act
        var result = await _controller.AddDocumentsToCollection(collectionId, request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region RemoveDocumentFromCollection Tests

    /// <summary>
    /// Test that RemoveDocumentFromCollection returns NoContent when successful.
    /// </summary>
    [Fact]
    public async Task RemoveDocumentFromCollection_WithValidData_ReturnsNoContent()
    {
        // Arrange
        var collectionId = 1L;
        var documentId = 1L;
        var collection = CreateTestCollection(collectionId);

        _collectionService
            .Setup(x => x.GetCollectionByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(collection);

        _collectionService
            .Setup(x => x.RemoveDocumentFromCollectionAsync(collectionId, documentId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.RemoveDocumentFromCollection(collectionId, documentId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    #endregion

    #region ReorderDocumentsInCollection Tests

    /// <summary>
    /// Test that ReorderDocumentsInCollection returns NoContent when successful.
    /// </summary>
    [Fact]
    public async Task ReorderDocumentsInCollection_WithValidData_ReturnsNoContent()
    {
        // Arrange
        var collectionId = 1L;
        var collection = CreateTestCollection(collectionId);
        var request = new ReorderDocumentsRequest
        {
            DocumentSortOrders = new Dictionary<long, int>
            {
                { 1L, 1 },
                { 2L, 2 },
                { 3L, 3 }
            }
        };

        _collectionService
            .Setup(x => x.GetCollectionByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(collection);

        _collectionService
            .Setup(x => x.ReorderDocumentsInCollectionAsync(
                collectionId, request.DocumentSortOrders, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ReorderDocumentsInCollection(collectionId, request);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    /// <summary>
    /// Test that ReorderDocumentsInCollection returns BadRequest when sort orders are empty.
    /// </summary>
    [Fact]
    public async Task ReorderDocumentsInCollection_WithEmptySortOrders_ReturnsBadRequest()
    {
        // Arrange
        var collectionId = 1L;
        var request = new ReorderDocumentsRequest
        {
            DocumentSortOrders = new Dictionary<long, int>()
        };

        // Act
        var result = await _controller.ReorderDocumentsInCollection(collectionId, request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region DeleteCollection Tests

    /// <summary>
    /// Test that DeleteCollection returns NoContent when successful.
    /// </summary>
    [Fact]
    public async Task DeleteCollection_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var collectionId = 1L;
        var collection = CreateTestCollection(collectionId);

        _collectionService
            .Setup(x => x.GetCollectionByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(collection);

        _collectionService
            .Setup(x => x.DeleteCollectionAsync(
                collectionId, _testUserId, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteCollection(collectionId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    /// <summary>
    /// Test that DeleteCollection returns NotFound when collection does not exist.
    /// </summary>
    [Fact]
    public async Task DeleteCollection_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var collectionId = 999L;
        _collectionService
            .Setup(x => x.GetCollectionByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Collection?)null);

        // Act
        var result = await _controller.DeleteCollection(collectionId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// Test that DeleteCollection includes deleted reason when provided.
    /// </summary>
    [Fact]
    public async Task DeleteCollection_WithDeletedReason_PassesReasonToService()
    {
        // Arrange
        var collectionId = 1L;
        var collection = CreateTestCollection(collectionId);
        var deletedReason = "No longer needed";

        _collectionService
            .Setup(x => x.GetCollectionByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(collection);

        _collectionService
            .Setup(x => x.DeleteCollectionAsync(
                collectionId, _testUserId, deletedReason, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteCollection(collectionId, deletedReason);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _collectionService.Verify(x => x.DeleteCollectionAsync(
            collectionId, _testUserId, deletedReason, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region RestoreCollection Tests

    /// <summary>
    /// Test that RestoreCollection returns NoContent when successful.
    /// </summary>
    [Fact]
    public async Task RestoreCollection_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var collectionId = 1L;
        var deletedCollection = CreateTestCollection(collectionId, isDeleted: true);
        var collections = new List<Collection> { deletedCollection };

        _collectionService
            .Setup(x => x.GetCollectionsByTenantAsync(_testTenantId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(collections);

        _collectionService
            .Setup(x => x.RestoreCollectionAsync(collectionId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.RestoreCollection(collectionId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    /// <summary>
    /// Test that RestoreCollection returns NotFound when collection does not exist.
    /// </summary>
    [Fact]
    public async Task RestoreCollection_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var collectionId = 999L;
        var collections = new List<Collection>();

        _collectionService
            .Setup(x => x.GetCollectionsByTenantAsync(_testTenantId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(collections);

        // Act
        var result = await _controller.RestoreCollection(collectionId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion
}
