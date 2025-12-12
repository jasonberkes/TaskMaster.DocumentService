using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskMaster.DocumentService.Api.Authorization;
using TaskMaster.DocumentService.Api.Extensions;
using TaskMaster.DocumentService.Core.Interfaces;

namespace TaskMaster.DocumentService.Api.Controllers;

/// <summary>
/// Controller for collection management operations including CRUD, publishing, and document relationships.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // Requires either JWT or API Key authentication
public class CollectionsController : ControllerBase
{
    private readonly ICollectionService _collectionService;
    private readonly ILogger<CollectionsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionsController"/> class.
    /// </summary>
    /// <param name="collectionService">The collection service.</param>
    /// <param name="logger">The logger.</param>
    public CollectionsController(
        ICollectionService collectionService,
        ILogger<CollectionsController> logger)
    {
        _collectionService = collectionService ?? throw new ArgumentNullException(nameof(collectionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new collection.
    /// </summary>
    /// <param name="request">The collection creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created collection.</returns>
    /// <response code="201">Returns the newly created collection.</response>
    /// <response code="400">If the request is invalid.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user does not have access to the specified tenant.</response>
    [HttpPost]
    [TenantAuthorization("tenantId")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateCollection(
        [FromBody] CreateCollectionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { error = "InvalidName", message = "Collection name is required." });
            }

            if (string.IsNullOrWhiteSpace(request.Slug))
            {
                return BadRequest(new { error = "InvalidSlug", message = "Collection slug is required." });
            }

            var createdBy = User.Identity?.Name ?? "system";

            _logger.LogInformation(
                "Creating collection '{Name}' with slug '{Slug}' for tenant {TenantId} by user {CreatedBy}",
                request.Name, request.Slug, request.TenantId, createdBy);

            var collection = await _collectionService.CreateCollectionAsync(
                request.TenantId,
                request.Name,
                request.Description,
                request.Slug,
                request.Metadata,
                request.Tags,
                createdBy,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetCollectionById),
                new { collectionId = collection.Id },
                new
                {
                    id = collection.Id,
                    tenantId = collection.TenantId,
                    name = collection.Name,
                    description = collection.Description,
                    slug = collection.Slug,
                    isPublished = collection.IsPublished,
                    publishedAt = collection.PublishedAt,
                    publishedBy = collection.PublishedBy,
                    metadata = collection.Metadata,
                    tags = collection.Tags,
                    sortOrder = collection.SortOrder,
                    createdAt = collection.CreatedAt,
                    createdBy = collection.CreatedBy
                });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when creating collection");
            return BadRequest(new { error = "InvalidOperation", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create collection");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while creating the collection." });
        }
    }

    /// <summary>
    /// Gets a collection by its identifier.
    /// </summary>
    /// <param name="collectionId">The collection identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The collection information.</returns>
    /// <response code="200">Returns the collection information.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the collection is not found.</response>
    [HttpGet("{collectionId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCollectionById(long collectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var collection = await _collectionService.GetCollectionByIdAsync(collectionId, cancellationToken);

            if (collection == null)
            {
                _logger.LogWarning("Collection {CollectionId} not found", collectionId);
                return NotFound(new { error = "CollectionNotFound", message = $"Collection with ID {collectionId} not found." });
            }

            // Verify tenant access
            var userTenantId = User.GetTenantId();
            if (collection.TenantId != userTenantId)
            {
                _logger.LogWarning(
                    "User from tenant {UserTenantId} attempted to access collection from tenant {CollectionTenantId}",
                    userTenantId, collection.TenantId);
                return NotFound(new { error = "CollectionNotFound", message = $"Collection with ID {collectionId} not found." });
            }

            return Ok(new
            {
                id = collection.Id,
                tenantId = collection.TenantId,
                name = collection.Name,
                description = collection.Description,
                slug = collection.Slug,
                isPublished = collection.IsPublished,
                publishedAt = collection.PublishedAt,
                publishedBy = collection.PublishedBy,
                metadata = collection.Metadata,
                tags = collection.Tags,
                sortOrder = collection.SortOrder,
                isDeleted = collection.IsDeleted,
                createdAt = collection.CreatedAt,
                createdBy = collection.CreatedBy,
                updatedAt = collection.UpdatedAt,
                updatedBy = collection.UpdatedBy
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve collection {CollectionId}", collectionId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while retrieving the collection." });
        }
    }

    /// <summary>
    /// Gets a collection by its slug.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="slug">The collection slug.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The collection information.</returns>
    /// <response code="200">Returns the collection information.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user does not have access to the specified tenant.</response>
    /// <response code="404">If the collection is not found.</response>
    [HttpGet("tenant/{tenantId}/slug/{slug}")]
    [TenantAuthorization("tenantId")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCollectionBySlug(
        int tenantId,
        string slug,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return BadRequest(new { error = "InvalidSlug", message = "Slug is required." });
            }

            _logger.LogDebug("Retrieving collection with slug '{Slug}' for tenant {TenantId}", slug, tenantId);

            var collection = await _collectionService.GetCollectionBySlugAsync(slug, tenantId, cancellationToken);

            if (collection == null)
            {
                _logger.LogWarning("Collection with slug '{Slug}' not found for tenant {TenantId}", slug, tenantId);
                return NotFound(new { error = "CollectionNotFound", message = $"Collection with slug '{slug}' not found." });
            }

            return Ok(new
            {
                id = collection.Id,
                tenantId = collection.TenantId,
                name = collection.Name,
                description = collection.Description,
                slug = collection.Slug,
                isPublished = collection.IsPublished,
                publishedAt = collection.PublishedAt,
                publishedBy = collection.PublishedBy,
                metadata = collection.Metadata,
                tags = collection.Tags,
                sortOrder = collection.SortOrder,
                isDeleted = collection.IsDeleted,
                createdAt = collection.CreatedAt,
                createdBy = collection.CreatedBy,
                updatedAt = collection.UpdatedAt,
                updatedBy = collection.UpdatedBy
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve collection with slug '{Slug}' for tenant {TenantId}", slug, tenantId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while retrieving the collection." });
        }
    }

    /// <summary>
    /// Gets all collections for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="includeDeleted">Whether to include soft-deleted collections.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of collections.</returns>
    /// <response code="200">Returns the list of collections.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user does not have access to the specified tenant.</response>
    [HttpGet("tenant/{tenantId}")]
    [TenantAuthorization("tenantId")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCollectionsByTenant(
        int tenantId,
        [FromQuery] bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Retrieving collections for tenant {TenantId}, includeDeleted: {IncludeDeleted}",
                tenantId, includeDeleted);

            var collections = await _collectionService.GetCollectionsByTenantAsync(tenantId, includeDeleted, cancellationToken);

            return Ok(collections.Select(c => new
            {
                id = c.Id,
                name = c.Name,
                description = c.Description,
                slug = c.Slug,
                isPublished = c.IsPublished,
                publishedAt = c.PublishedAt,
                publishedBy = c.PublishedBy,
                sortOrder = c.SortOrder,
                isDeleted = c.IsDeleted,
                createdAt = c.CreatedAt,
                createdBy = c.CreatedBy,
                updatedAt = c.UpdatedAt,
                updatedBy = c.UpdatedBy
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve collections for tenant {TenantId}", tenantId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while retrieving collections." });
        }
    }

    /// <summary>
    /// Gets all published collections for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of published collections.</returns>
    /// <response code="200">Returns the list of published collections.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user does not have access to the specified tenant.</response>
    [HttpGet("tenant/{tenantId}/published")]
    [TenantAuthorization("tenantId")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPublishedCollections(
        int tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving published collections for tenant {TenantId}", tenantId);

            var collections = await _collectionService.GetPublishedCollectionsAsync(tenantId, cancellationToken);

            return Ok(collections.Select(c => new
            {
                id = c.Id,
                name = c.Name,
                description = c.Description,
                slug = c.Slug,
                publishedAt = c.PublishedAt,
                publishedBy = c.PublishedBy,
                metadata = c.Metadata,
                tags = c.Tags,
                sortOrder = c.SortOrder,
                createdAt = c.CreatedAt
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve published collections for tenant {TenantId}", tenantId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while retrieving published collections." });
        }
    }

    /// <summary>
    /// Updates collection metadata and properties.
    /// </summary>
    /// <param name="collectionId">The collection identifier.</param>
    /// <param name="request">The collection update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated collection.</returns>
    /// <response code="200">Returns the updated collection.</response>
    /// <response code="400">If the request is invalid.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the collection is not found.</response>
    [HttpPut("{collectionId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCollection(
        long collectionId,
        [FromBody] UpdateCollectionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // First, verify the collection exists and belongs to user's tenant
            var existingCollection = await _collectionService.GetCollectionByIdAsync(collectionId, cancellationToken);
            if (existingCollection == null)
            {
                return NotFound(new { error = "CollectionNotFound", message = $"Collection with ID {collectionId} not found." });
            }

            var userTenantId = User.GetTenantId();
            if (existingCollection.TenantId != userTenantId)
            {
                return NotFound(new { error = "CollectionNotFound", message = $"Collection with ID {collectionId} not found." });
            }

            var updatedBy = User.Identity?.Name ?? "system";

            var collection = await _collectionService.UpdateCollectionAsync(
                collectionId,
                request.Name,
                request.Description,
                request.Slug,
                request.Metadata,
                request.Tags,
                updatedBy,
                cancellationToken);

            return Ok(new
            {
                id = collection.Id,
                tenantId = collection.TenantId,
                name = collection.Name,
                description = collection.Description,
                slug = collection.Slug,
                isPublished = collection.IsPublished,
                metadata = collection.Metadata,
                tags = collection.Tags,
                sortOrder = collection.SortOrder,
                updatedAt = collection.UpdatedAt,
                updatedBy = collection.UpdatedBy
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when updating collection");
            return BadRequest(new { error = "InvalidOperation", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update collection {CollectionId}", collectionId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while updating the collection." });
        }
    }

    /// <summary>
    /// Publishes a collection.
    /// </summary>
    /// <param name="collectionId">The collection identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The published collection.</returns>
    /// <response code="200">Returns the published collection.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the collection is not found.</response>
    [HttpPost("{collectionId}/publish")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PublishCollection(
        long collectionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify the collection exists and belongs to user's tenant
            var existingCollection = await _collectionService.GetCollectionByIdAsync(collectionId, cancellationToken);
            if (existingCollection == null)
            {
                return NotFound(new { error = "CollectionNotFound", message = $"Collection with ID {collectionId} not found." });
            }

            var userTenantId = User.GetTenantId();
            if (existingCollection.TenantId != userTenantId)
            {
                return NotFound(new { error = "CollectionNotFound", message = $"Collection with ID {collectionId} not found." });
            }

            var publishedBy = User.Identity?.Name ?? "system";

            var collection = await _collectionService.PublishCollectionAsync(collectionId, publishedBy, cancellationToken);

            return Ok(new
            {
                id = collection.Id,
                tenantId = collection.TenantId,
                name = collection.Name,
                slug = collection.Slug,
                isPublished = collection.IsPublished,
                publishedAt = collection.PublishedAt,
                publishedBy = collection.PublishedBy,
                updatedAt = collection.UpdatedAt,
                updatedBy = collection.UpdatedBy
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when publishing collection");
            return BadRequest(new { error = "InvalidOperation", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish collection {CollectionId}", collectionId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while publishing the collection." });
        }
    }

    /// <summary>
    /// Unpublishes a collection.
    /// </summary>
    /// <param name="collectionId">The collection identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The unpublished collection.</returns>
    /// <response code="200">Returns the unpublished collection.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the collection is not found.</response>
    [HttpPost("{collectionId}/unpublish")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnpublishCollection(
        long collectionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify the collection exists and belongs to user's tenant
            var existingCollection = await _collectionService.GetCollectionByIdAsync(collectionId, cancellationToken);
            if (existingCollection == null)
            {
                return NotFound(new { error = "CollectionNotFound", message = $"Collection with ID {collectionId} not found." });
            }

            var userTenantId = User.GetTenantId();
            if (existingCollection.TenantId != userTenantId)
            {
                return NotFound(new { error = "CollectionNotFound", message = $"Collection with ID {collectionId} not found." });
            }

            var collection = await _collectionService.UnpublishCollectionAsync(collectionId, cancellationToken);

            return Ok(new
            {
                id = collection.Id,
                tenantId = collection.TenantId,
                name = collection.Name,
                slug = collection.Slug,
                isPublished = collection.IsPublished,
                updatedAt = collection.UpdatedAt
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when unpublishing collection");
            return BadRequest(new { error = "InvalidOperation", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unpublish collection {CollectionId}", collectionId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while unpublishing the collection." });
        }
    }

    /// <summary>
    /// Gets all documents in a collection.
    /// </summary>
    /// <param name="collectionId">The collection identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of documents.</returns>
    /// <response code="200">Returns the list of documents in the collection.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the collection is not found.</response>
    [HttpGet("{collectionId}/documents")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDocumentsInCollection(
        long collectionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify the collection exists and belongs to user's tenant
            var collection = await _collectionService.GetCollectionByIdAsync(collectionId, cancellationToken);
            if (collection == null)
            {
                return NotFound(new { error = "CollectionNotFound", message = $"Collection with ID {collectionId} not found." });
            }

            var userTenantId = User.GetTenantId();
            if (collection.TenantId != userTenantId)
            {
                return NotFound(new { error = "CollectionNotFound", message = $"Collection with ID {collectionId} not found." });
            }

            var documents = await _collectionService.GetDocumentsInCollectionAsync(collectionId, cancellationToken);

            return Ok(documents.Select(d => new
            {
                id = d.Id,
                documentTypeId = d.DocumentTypeId,
                title = d.Title,
                description = d.Description,
                originalFileName = d.OriginalFileName,
                mimeType = d.MimeType,
                fileSizeBytes = d.FileSizeBytes,
                version = d.Version,
                isCurrentVersion = d.IsCurrentVersion,
                createdAt = d.CreatedAt,
                createdBy = d.CreatedBy
            }));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when retrieving documents in collection");
            return BadRequest(new { error = "InvalidOperation", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve documents for collection {CollectionId}", collectionId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while retrieving documents in the collection." });
        }
    }

    /// <summary>
    /// Adds a document to a collection.
    /// </summary>
    /// <param name="collectionId">The collection identifier.</param>
    /// <param name="request">The add document request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    /// <response code="204">Document successfully added to collection.</response>
    /// <response code="400">If the request is invalid.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the collection is not found.</response>
    [HttpPost("{collectionId}/documents")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddDocumentToCollection(
        long collectionId,
        [FromBody] AddDocumentToCollectionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify the collection exists and belongs to user's tenant
            var collection = await _collectionService.GetCollectionByIdAsync(collectionId, cancellationToken);
            if (collection == null)
            {
                return NotFound(new { error = "CollectionNotFound", message = $"Collection with ID {collectionId} not found." });
            }

            var userTenantId = User.GetTenantId();
            if (collection.TenantId != userTenantId)
            {
                return NotFound(new { error = "CollectionNotFound", message = $"Collection with ID {collectionId} not found." });
            }

            var addedBy = User.Identity?.Name ?? "system";

            await _collectionService.AddDocumentToCollectionAsync(
                collectionId,
                request.DocumentId,
                request.SortOrder,
                addedBy,
                request.Metadata,
                cancellationToken);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when adding document to collection");
            return BadRequest(new { error = "InvalidOperation", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add document to collection {CollectionId}", collectionId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while adding the document to the collection." });
        }
    }

    /// <summary>
    /// Adds multiple documents to a collection.
    /// </summary>
    /// <param name="collectionId">The collection identifier.</param>
    /// <param name="request">The add documents request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    /// <response code="204">Documents successfully added to collection.</response>
    /// <response code="400">If the request is invalid.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the collection is not found.</response>
    [HttpPost("{collectionId}/documents/batch")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddDocumentsToCollection(
        long collectionId,
        [FromBody] AddDocumentsToCollectionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.DocumentIds == null || !request.DocumentIds.Any())
            {
                return BadRequest(new { error = "InvalidRequest", message = "Document IDs are required." });
            }

            // Verify the collection exists and belongs to user's tenant
            var collection = await _collectionService.GetCollectionByIdAsync(collectionId, cancellationToken);
            if (collection == null)
            {
                return NotFound(new { error = "CollectionNotFound", message = $"Collection with ID {collectionId} not found." });
            }

            var userTenantId = User.GetTenantId();
            if (collection.TenantId != userTenantId)
            {
                return NotFound(new { error = "CollectionNotFound", message = $"Collection with ID {collectionId} not found." });
            }

            var addedBy = User.Identity?.Name ?? "system";

            await _collectionService.AddDocumentsToCollectionAsync(
                collectionId,
                request.DocumentIds,
                addedBy,
                cancellationToken);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when adding documents to collection");
            return BadRequest(new { error = "InvalidOperation", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add documents to collection {CollectionId}", collectionId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while adding documents to the collection." });
        }
    }

    /// <summary>
    /// Removes a document from a collection.
    /// </summary>
    /// <param name="collectionId">The collection identifier.</param>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    /// <response code="204">Document successfully removed from collection.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the collection is not found.</response>
    [HttpDelete("{collectionId}/documents/{documentId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveDocumentFromCollection(
        long collectionId,
        long documentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify the collection exists and belongs to user's tenant
            var collection = await _collectionService.GetCollectionByIdAsync(collectionId, cancellationToken);
            if (collection == null)
            {
                return NotFound(new { error = "CollectionNotFound", message = $"Collection with ID {collectionId} not found." });
            }

            var userTenantId = User.GetTenantId();
            if (collection.TenantId != userTenantId)
            {
                return NotFound(new { error = "CollectionNotFound", message = $"Collection with ID {collectionId} not found." });
            }

            await _collectionService.RemoveDocumentFromCollectionAsync(collectionId, documentId, cancellationToken);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when removing document from collection");
            return BadRequest(new { error = "InvalidOperation", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove document from collection {CollectionId}", collectionId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while removing the document from the collection." });
        }
    }

    /// <summary>
    /// Reorders documents in a collection.
    /// </summary>
    /// <param name="collectionId">The collection identifier.</param>
    /// <param name="request">The reorder request with document sort orders.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    /// <response code="204">Documents successfully reordered.</response>
    /// <response code="400">If the request is invalid.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the collection is not found.</response>
    [HttpPut("{collectionId}/documents/reorder")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReorderDocumentsInCollection(
        long collectionId,
        [FromBody] ReorderDocumentsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.DocumentSortOrders == null || !request.DocumentSortOrders.Any())
            {
                return BadRequest(new { error = "InvalidRequest", message = "Document sort orders are required." });
            }

            // Verify the collection exists and belongs to user's tenant
            var collection = await _collectionService.GetCollectionByIdAsync(collectionId, cancellationToken);
            if (collection == null)
            {
                return NotFound(new { error = "CollectionNotFound", message = $"Collection with ID {collectionId} not found." });
            }

            var userTenantId = User.GetTenantId();
            if (collection.TenantId != userTenantId)
            {
                return NotFound(new { error = "CollectionNotFound", message = $"Collection with ID {collectionId} not found." });
            }

            await _collectionService.ReorderDocumentsInCollectionAsync(
                collectionId,
                request.DocumentSortOrders,
                cancellationToken);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when reordering documents in collection");
            return BadRequest(new { error = "InvalidOperation", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reorder documents in collection {CollectionId}", collectionId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while reordering documents in the collection." });
        }
    }

    /// <summary>
    /// Soft-deletes a collection.
    /// </summary>
    /// <param name="collectionId">The collection identifier.</param>
    /// <param name="deletedReason">Optional reason for deletion.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    /// <response code="204">Collection successfully deleted.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the collection is not found.</response>
    [HttpDelete("{collectionId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCollection(
        long collectionId,
        [FromQuery] string? deletedReason = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify the collection exists and belongs to user's tenant
            var collection = await _collectionService.GetCollectionByIdAsync(collectionId, cancellationToken);
            if (collection == null)
            {
                return NotFound(new { error = "CollectionNotFound", message = $"Collection with ID {collectionId} not found." });
            }

            var userTenantId = User.GetTenantId();
            if (collection.TenantId != userTenantId)
            {
                return NotFound(new { error = "CollectionNotFound", message = $"Collection with ID {collectionId} not found." });
            }

            var deletedBy = User.Identity?.Name ?? "system";

            await _collectionService.DeleteCollectionAsync(collectionId, deletedBy, deletedReason, cancellationToken);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when deleting collection");
            return BadRequest(new { error = "InvalidOperation", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete collection {CollectionId}", collectionId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while deleting the collection." });
        }
    }

    /// <summary>
    /// Restores a soft-deleted collection.
    /// </summary>
    /// <param name="collectionId">The collection identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    /// <response code="204">Collection successfully restored.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the collection is not found.</response>
    [HttpPost("{collectionId}/restore")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestoreCollection(long collectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify the collection exists and belongs to user's tenant (need to check with includeDeleted)
            var userTenantId = User.GetTenantId() ?? 0;
            var collections = await _collectionService.GetCollectionsByTenantAsync(userTenantId, true, cancellationToken);
            var collection = collections.FirstOrDefault(c => c.Id == collectionId);

            if (collection == null)
            {
                return NotFound(new { error = "CollectionNotFound", message = $"Collection with ID {collectionId} not found." });
            }

            await _collectionService.RestoreCollectionAsync(collectionId, cancellationToken);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when restoring collection");
            return BadRequest(new { error = "InvalidOperation", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore collection {CollectionId}", collectionId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while restoring the collection." });
        }
    }
}

/// <summary>
/// Request model for creating a collection.
/// </summary>
public class CreateCollectionRequest
{
    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    public int TenantId { get; set; }

    /// <summary>
    /// Gets or sets the collection name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the collection description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the URL-friendly slug.
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the metadata as JSON string.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the tags as JSON string.
    /// </summary>
    public string? Tags { get; set; }
}

/// <summary>
/// Request model for updating a collection.
/// </summary>
public class UpdateCollectionRequest
{
    /// <summary>
    /// Gets or sets the updated name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the updated description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the updated slug.
    /// </summary>
    public string? Slug { get; set; }

    /// <summary>
    /// Gets or sets the updated metadata as JSON string.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the updated tags as JSON string.
    /// </summary>
    public string? Tags { get; set; }
}

/// <summary>
/// Request model for adding a document to a collection.
/// </summary>
public class AddDocumentToCollectionRequest
{
    /// <summary>
    /// Gets or sets the document identifier.
    /// </summary>
    public long DocumentId { get; set; }

    /// <summary>
    /// Gets or sets the sort order for the document.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Gets or sets optional metadata as JSON string.
    /// </summary>
    public string? Metadata { get; set; }
}

/// <summary>
/// Request model for adding multiple documents to a collection.
/// </summary>
public class AddDocumentsToCollectionRequest
{
    /// <summary>
    /// Gets or sets the document identifiers to add.
    /// </summary>
    public IEnumerable<long> DocumentIds { get; set; } = new List<long>();
}

/// <summary>
/// Request model for reordering documents in a collection.
/// </summary>
public class ReorderDocumentsRequest
{
    /// <summary>
    /// Gets or sets the dictionary mapping document IDs to their new sort orders.
    /// </summary>
    public Dictionary<long, int> DocumentSortOrders { get; set; } = new Dictionary<long, int>();
}
