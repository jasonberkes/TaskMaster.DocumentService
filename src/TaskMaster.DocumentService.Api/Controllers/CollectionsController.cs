using Microsoft.AspNetCore.Mvc;
using TaskMaster.DocumentService.Core.DTOs;
using TaskMaster.DocumentService.Core.Interfaces;

namespace TaskMaster.DocumentService.Api.Controllers;

/// <summary>
/// API controller for managing document collections.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CollectionsController : ControllerBase
{
    private readonly ICollectionService _collectionService;
    private readonly ILogger<CollectionsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionsController"/> class.
    /// </summary>
    /// <param name="collectionService">The collection service.</param>
    /// <param name="logger">The logger instance.</param>
    public CollectionsController(ICollectionService collectionService, ILogger<CollectionsController> logger)
    {
        _collectionService = collectionService ?? throw new ArgumentNullException(nameof(collectionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets a collection by ID.
    /// </summary>
    /// <param name="id">The collection ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The collection if found.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CollectionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CollectionDto>> GetById(long id, CancellationToken cancellationToken)
    {
        try
        {
            var collection = await _collectionService.GetByIdAsync(id, cancellationToken);
            if (collection == null)
            {
                return NotFound($"Collection with ID {id} not found.");
            }

            return Ok(collection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving collection {CollectionId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the collection.");
        }
    }

    /// <summary>
    /// Gets a collection by tenant ID and slug.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="slug">The collection slug.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The collection if found.</returns>
    [HttpGet("tenant/{tenantId}/slug/{slug}")]
    [ProducesResponseType(typeof(CollectionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CollectionDto>> GetBySlug(int tenantId, string slug, CancellationToken cancellationToken)
    {
        try
        {
            var collection = await _collectionService.GetBySlugAsync(tenantId, slug, cancellationToken);
            if (collection == null)
            {
                return NotFound($"Collection with slug '{slug}' not found for tenant {tenantId}.");
            }

            return Ok(collection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving collection by slug {Slug} for tenant {TenantId}", slug, tenantId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the collection.");
        }
    }

    /// <summary>
    /// Gets all collections for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="includeDeleted">Whether to include soft-deleted collections.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of collections.</returns>
    [HttpGet("tenant/{tenantId}")]
    [ProducesResponseType(typeof(List<CollectionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<CollectionDto>>> GetByTenantId(int tenantId, [FromQuery] bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var collections = await _collectionService.GetByTenantIdAsync(tenantId, includeDeleted, cancellationToken);
            return Ok(collections);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving collections for tenant {TenantId}", tenantId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving collections.");
        }
    }

    /// <summary>
    /// Gets published collections for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of published collections.</returns>
    [HttpGet("tenant/{tenantId}/published")]
    [ProducesResponseType(typeof(List<CollectionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<CollectionDto>>> GetPublishedByTenantId(int tenantId, CancellationToken cancellationToken)
    {
        try
        {
            var collections = await _collectionService.GetPublishedByTenantIdAsync(tenantId, cancellationToken);
            return Ok(collections);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving published collections for tenant {TenantId}", tenantId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving published collections.");
        }
    }

    /// <summary>
    /// Creates a new collection.
    /// </summary>
    /// <param name="createDto">The collection creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created collection.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CollectionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CollectionDto>> Create([FromBody] CreateCollectionDto createDto, CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // In a real application, get the user from authentication context
            var createdBy = User.Identity?.Name ?? "system";

            var collection = await _collectionService.CreateAsync(createDto, createdBy, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = collection.Id }, collection);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while creating collection");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating collection");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the collection.");
        }
    }

    /// <summary>
    /// Updates an existing collection.
    /// </summary>
    /// <param name="id">The collection ID.</param>
    /// <param name="updateDto">The collection update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated collection.</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(CollectionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CollectionDto>> Update(long id, [FromBody] UpdateCollectionDto updateDto, CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // In a real application, get the user from authentication context
            var updatedBy = User.Identity?.Name ?? "system";

            var collection = await _collectionService.UpdateAsync(id, updateDto, updatedBy, cancellationToken);
            if (collection == null)
            {
                return NotFound($"Collection with ID {id} not found.");
            }

            return Ok(collection);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while updating collection {CollectionId}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating collection {CollectionId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the collection.");
        }
    }

    /// <summary>
    /// Deletes a collection (soft delete).
    /// </summary>
    /// <param name="id">The collection ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        try
        {
            // In a real application, get the user from authentication context
            var deletedBy = User.Identity?.Name ?? "system";

            var result = await _collectionService.DeleteAsync(id, deletedBy, cancellationToken);
            if (!result)
            {
                return NotFound($"Collection with ID {id} not found.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting collection {CollectionId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the collection.");
        }
    }

    /// <summary>
    /// Publishes a collection.
    /// </summary>
    /// <param name="id">The collection ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpPost("{id}/publish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Publish(long id, CancellationToken cancellationToken)
    {
        try
        {
            // In a real application, get the user from authentication context
            var publishedBy = User.Identity?.Name ?? "system";

            var result = await _collectionService.PublishAsync(id, publishedBy, cancellationToken);
            if (!result)
            {
                return NotFound($"Collection with ID {id} not found.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing collection {CollectionId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while publishing the collection.");
        }
    }

    /// <summary>
    /// Unpublishes a collection.
    /// </summary>
    /// <param name="id">The collection ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpPost("{id}/unpublish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Unpublish(long id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _collectionService.UnpublishAsync(id, cancellationToken);
            if (!result)
            {
                return NotFound($"Collection with ID {id} not found.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unpublishing collection {CollectionId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while unpublishing the collection.");
        }
    }

    /// <summary>
    /// Adds a document to a collection.
    /// </summary>
    /// <param name="id">The collection ID.</param>
    /// <param name="addDocumentDto">The document addition data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created collection-document relationship.</returns>
    [HttpPost("{id}/documents")]
    [ProducesResponseType(typeof(CollectionDocumentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CollectionDocumentDto>> AddDocument(long id, [FromBody] AddDocumentToCollectionDto addDocumentDto, CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // In a real application, get the user from authentication context
            var addedBy = User.Identity?.Name ?? "system";

            var collectionDocument = await _collectionService.AddDocumentAsync(id, addDocumentDto, addedBy, cancellationToken);
            if (collectionDocument == null)
            {
                return NotFound($"Collection with ID {id} not found.");
            }

            return CreatedAtAction(nameof(GetDocuments), new { id }, collectionDocument);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while adding document to collection {CollectionId}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding document to collection {CollectionId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while adding the document to the collection.");
        }
    }

    /// <summary>
    /// Removes a document from a collection.
    /// </summary>
    /// <param name="id">The collection ID.</param>
    /// <param name="documentId">The document ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpDelete("{id}/documents/{documentId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RemoveDocument(long id, long documentId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _collectionService.RemoveDocumentAsync(id, documentId, cancellationToken);
            if (!result)
            {
                return NotFound($"Document {documentId} not found in collection {id}.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing document {DocumentId} from collection {CollectionId}", documentId, id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while removing the document from the collection.");
        }
    }

    /// <summary>
    /// Gets all documents in a collection.
    /// </summary>
    /// <param name="id">The collection ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of documents in the collection.</returns>
    [HttpGet("{id}/documents")]
    [ProducesResponseType(typeof(List<CollectionDocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<CollectionDocumentDto>>> GetDocuments(long id, CancellationToken cancellationToken)
    {
        try
        {
            var documents = await _collectionService.GetDocumentsAsync(id, cancellationToken);
            return Ok(documents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving documents for collection {CollectionId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving documents.");
        }
    }
}
