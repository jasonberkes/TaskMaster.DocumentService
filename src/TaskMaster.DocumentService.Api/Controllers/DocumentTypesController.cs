using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Interfaces;

namespace TaskMaster.DocumentService.Api.Controllers;

/// <summary>
/// Controller for document type management operations.
/// Provides endpoints for managing document types that define document schemas and metadata.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // Requires either JWT or API Key authentication
public class DocumentTypesController : ControllerBase
{
    private readonly IDocumentTypeService _documentTypeService;
    private readonly ILogger<DocumentTypesController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentTypesController"/> class.
    /// </summary>
    /// <param name="documentTypeService">The document type service.</param>
    /// <param name="logger">The logger.</param>
    public DocumentTypesController(
        IDocumentTypeService documentTypeService,
        ILogger<DocumentTypesController> logger)
    {
        _documentTypeService = documentTypeService ?? throw new ArgumentNullException(nameof(documentTypeService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all document types.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of all document types.</returns>
    /// <response code="200">Returns the list of document types.</response>
    /// <response code="401">If the user is not authenticated.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAllDocumentTypes(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving all document types");

            var documentTypes = await _documentTypeService.GetAllAsync(cancellationToken);

            return Ok(documentTypes.Select(dt => new
            {
                id = dt.Id,
                name = dt.Name,
                displayName = dt.DisplayName,
                description = dt.Description,
                metadataSchema = dt.MetadataSchema,
                defaultTags = dt.DefaultTags,
                icon = dt.Icon,
                isContentIndexed = dt.IsContentIndexed,
                hasExtensionTable = dt.HasExtensionTable,
                extensionTableName = dt.ExtensionTableName,
                isActive = dt.IsActive,
                createdAt = dt.CreatedAt
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve document types");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while retrieving document types." });
        }
    }

    /// <summary>
    /// Gets active document types only.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of active document types.</returns>
    /// <response code="200">Returns the list of active document types.</response>
    /// <response code="401">If the user is not authenticated.</response>
    [HttpGet("active")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetActiveDocumentTypes(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving active document types");

            var documentTypes = await _documentTypeService.GetActiveDocumentTypesAsync(cancellationToken);

            return Ok(documentTypes.Select(dt => new
            {
                id = dt.Id,
                name = dt.Name,
                displayName = dt.DisplayName,
                description = dt.Description,
                metadataSchema = dt.MetadataSchema,
                defaultTags = dt.DefaultTags,
                icon = dt.Icon,
                isContentIndexed = dt.IsContentIndexed,
                hasExtensionTable = dt.HasExtensionTable,
                extensionTableName = dt.ExtensionTableName,
                createdAt = dt.CreatedAt
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve active document types");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while retrieving active document types." });
        }
    }

    /// <summary>
    /// Gets document types that have content indexing enabled.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of indexable document types.</returns>
    /// <response code="200">Returns the list of indexable document types.</response>
    /// <response code="401">If the user is not authenticated.</response>
    [HttpGet("indexable")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetIndexableDocumentTypes(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving indexable document types");

            var documentTypes = await _documentTypeService.GetIndexableTypesAsync(cancellationToken);

            return Ok(documentTypes.Select(dt => new
            {
                id = dt.Id,
                name = dt.Name,
                displayName = dt.DisplayName,
                description = dt.Description,
                isContentIndexed = dt.IsContentIndexed,
                isActive = dt.IsActive
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve indexable document types");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while retrieving indexable document types." });
        }
    }

    /// <summary>
    /// Gets document types that have extension tables.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of document types with extension tables.</returns>
    /// <response code="200">Returns the list of document types with extension tables.</response>
    /// <response code="401">If the user is not authenticated.</response>
    [HttpGet("with-extension-tables")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetDocumentTypesWithExtensionTables(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving document types with extension tables");

            var documentTypes = await _documentTypeService.GetTypesWithExtensionTablesAsync(cancellationToken);

            return Ok(documentTypes.Select(dt => new
            {
                id = dt.Id,
                name = dt.Name,
                displayName = dt.DisplayName,
                hasExtensionTable = dt.HasExtensionTable,
                extensionTableName = dt.ExtensionTableName,
                isActive = dt.IsActive
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve document types with extension tables");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while retrieving document types with extension tables." });
        }
    }

    /// <summary>
    /// Gets a document type by its identifier.
    /// </summary>
    /// <param name="id">The document type identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The document type information.</returns>
    /// <response code="200">Returns the document type information.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the document type is not found.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDocumentTypeById(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving document type {DocumentTypeId}", id);

            var documentType = await _documentTypeService.GetByIdAsync(id, cancellationToken);

            if (documentType == null)
            {
                _logger.LogWarning("Document type {DocumentTypeId} not found", id);
                return NotFound(new { error = "DocumentTypeNotFound", message = $"Document type with ID {id} not found." });
            }

            return Ok(new
            {
                id = documentType.Id,
                name = documentType.Name,
                displayName = documentType.DisplayName,
                description = documentType.Description,
                metadataSchema = documentType.MetadataSchema,
                defaultTags = documentType.DefaultTags,
                icon = documentType.Icon,
                isContentIndexed = documentType.IsContentIndexed,
                hasExtensionTable = documentType.HasExtensionTable,
                extensionTableName = documentType.ExtensionTableName,
                isActive = documentType.IsActive,
                createdAt = documentType.CreatedAt
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when retrieving document type");
            return BadRequest(new { error = "InvalidArgument", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve document type {DocumentTypeId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while retrieving the document type." });
        }
    }

    /// <summary>
    /// Gets a document type by its name.
    /// </summary>
    /// <param name="name">The document type name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The document type information.</returns>
    /// <response code="200">Returns the document type information.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the document type is not found.</response>
    [HttpGet("by-name/{name}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDocumentTypeByName(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving document type by name: {Name}", name);

            var documentType = await _documentTypeService.GetByNameAsync(name, cancellationToken);

            if (documentType == null)
            {
                _logger.LogWarning("Document type with name {Name} not found", name);
                return NotFound(new { error = "DocumentTypeNotFound", message = $"Document type with name '{name}' not found." });
            }

            return Ok(new
            {
                id = documentType.Id,
                name = documentType.Name,
                displayName = documentType.DisplayName,
                description = documentType.Description,
                metadataSchema = documentType.MetadataSchema,
                defaultTags = documentType.DefaultTags,
                icon = documentType.Icon,
                isContentIndexed = documentType.IsContentIndexed,
                hasExtensionTable = documentType.HasExtensionTable,
                extensionTableName = documentType.ExtensionTableName,
                isActive = documentType.IsActive,
                createdAt = documentType.CreatedAt
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when retrieving document type by name");
            return BadRequest(new { error = "InvalidArgument", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve document type by name: {Name}", name);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while retrieving the document type." });
        }
    }

    /// <summary>
    /// Creates a new document type.
    /// </summary>
    /// <param name="request">The document type creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created document type.</returns>
    /// <response code="201">Returns the newly created document type.</response>
    /// <response code="400">If the request is invalid.</response>
    /// <response code="401">If the user is not authenticated.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateDocumentType(
        [FromBody] CreateDocumentTypeRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { error = "InvalidName", message = "Name is required." });
            }

            if (string.IsNullOrWhiteSpace(request.DisplayName))
            {
                return BadRequest(new { error = "InvalidDisplayName", message = "Display name is required." });
            }

            _logger.LogInformation("Creating document type '{Name}'", request.Name);

            var documentType = new DocumentType
            {
                Name = request.Name,
                DisplayName = request.DisplayName,
                Description = request.Description,
                MetadataSchema = request.MetadataSchema,
                DefaultTags = request.DefaultTags,
                Icon = request.Icon,
                IsContentIndexed = request.IsContentIndexed,
                HasExtensionTable = request.HasExtensionTable,
                ExtensionTableName = request.ExtensionTableName,
                IsActive = request.IsActive
            };

            var createdDocumentType = await _documentTypeService.CreateAsync(documentType, cancellationToken);

            return CreatedAtAction(
                nameof(GetDocumentTypeById),
                new { id = createdDocumentType.Id },
                new
                {
                    id = createdDocumentType.Id,
                    name = createdDocumentType.Name,
                    displayName = createdDocumentType.DisplayName,
                    description = createdDocumentType.Description,
                    metadataSchema = createdDocumentType.MetadataSchema,
                    defaultTags = createdDocumentType.DefaultTags,
                    icon = createdDocumentType.Icon,
                    isContentIndexed = createdDocumentType.IsContentIndexed,
                    hasExtensionTable = createdDocumentType.HasExtensionTable,
                    extensionTableName = createdDocumentType.ExtensionTableName,
                    isActive = createdDocumentType.IsActive,
                    createdAt = createdDocumentType.CreatedAt
                });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when creating document type");
            return BadRequest(new { error = "InvalidOperation", message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when creating document type");
            return BadRequest(new { error = "InvalidArgument", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create document type");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while creating the document type." });
        }
    }

    /// <summary>
    /// Updates an existing document type.
    /// </summary>
    /// <param name="id">The document type identifier.</param>
    /// <param name="request">The document type update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated document type.</returns>
    /// <response code="200">Returns the updated document type.</response>
    /// <response code="400">If the request is invalid.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the document type is not found.</response>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDocumentType(
        int id,
        [FromBody] UpdateDocumentTypeRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { error = "InvalidName", message = "Name is required." });
            }

            if (string.IsNullOrWhiteSpace(request.DisplayName))
            {
                return BadRequest(new { error = "InvalidDisplayName", message = "Display name is required." });
            }

            _logger.LogInformation("Updating document type {DocumentTypeId}", id);

            var documentType = new DocumentType
            {
                Id = id,
                Name = request.Name,
                DisplayName = request.DisplayName,
                Description = request.Description,
                MetadataSchema = request.MetadataSchema,
                DefaultTags = request.DefaultTags,
                Icon = request.Icon,
                IsContentIndexed = request.IsContentIndexed,
                HasExtensionTable = request.HasExtensionTable,
                ExtensionTableName = request.ExtensionTableName,
                IsActive = request.IsActive
            };

            var updatedDocumentType = await _documentTypeService.UpdateAsync(documentType, cancellationToken);

            return Ok(new
            {
                id = updatedDocumentType.Id,
                name = updatedDocumentType.Name,
                displayName = updatedDocumentType.DisplayName,
                description = updatedDocumentType.Description,
                metadataSchema = updatedDocumentType.MetadataSchema,
                defaultTags = updatedDocumentType.DefaultTags,
                icon = updatedDocumentType.Icon,
                isContentIndexed = updatedDocumentType.IsContentIndexed,
                hasExtensionTable = updatedDocumentType.HasExtensionTable,
                extensionTableName = updatedDocumentType.ExtensionTableName,
                isActive = updatedDocumentType.IsActive,
                createdAt = updatedDocumentType.CreatedAt
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when updating document type");

            if (ex.Message.Contains("does not exist"))
            {
                return NotFound(new { error = "DocumentTypeNotFound", message = ex.Message });
            }

            return BadRequest(new { error = "InvalidOperation", message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when updating document type");
            return BadRequest(new { error = "InvalidArgument", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update document type {DocumentTypeId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while updating the document type." });
        }
    }

    /// <summary>
    /// Deletes a document type by its identifier.
    /// </summary>
    /// <param name="id">The document type identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    /// <response code="204">Document type successfully deleted.</response>
    /// <response code="400">If the document type is in use by documents.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the document type is not found.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDocumentType(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting document type {DocumentTypeId}", id);

            var deleted = await _documentTypeService.DeleteAsync(id, cancellationToken);

            if (!deleted)
            {
                _logger.LogWarning("Document type {DocumentTypeId} not found", id);
                return NotFound(new { error = "DocumentTypeNotFound", message = $"Document type with ID {id} not found." });
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when deleting document type");
            return BadRequest(new { error = "InvalidOperation", message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when deleting document type");
            return BadRequest(new { error = "InvalidArgument", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete document type {DocumentTypeId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while deleting the document type." });
        }
    }
}

/// <summary>
/// Request model for creating a document type.
/// </summary>
public class CreateDocumentTypeRequest
{
    /// <summary>
    /// Gets or sets the internal name of the document type.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the document type.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the document type.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the metadata schema as JSON.
    /// </summary>
    public string? MetadataSchema { get; set; }

    /// <summary>
    /// Gets or sets the default tags as JSON.
    /// </summary>
    public string? DefaultTags { get; set; }

    /// <summary>
    /// Gets or sets the icon identifier for the document type.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether content should be indexed.
    /// </summary>
    public bool IsContentIndexed { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this type has an extension table.
    /// </summary>
    public bool HasExtensionTable { get; set; }

    /// <summary>
    /// Gets or sets the extension table name if applicable.
    /// </summary>
    public string? ExtensionTableName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the document type is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Request model for updating a document type.
/// </summary>
public class UpdateDocumentTypeRequest
{
    /// <summary>
    /// Gets or sets the internal name of the document type.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the document type.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the document type.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the metadata schema as JSON.
    /// </summary>
    public string? MetadataSchema { get; set; }

    /// <summary>
    /// Gets or sets the default tags as JSON.
    /// </summary>
    public string? DefaultTags { get; set; }

    /// <summary>
    /// Gets or sets the icon identifier for the document type.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether content should be indexed.
    /// </summary>
    public bool IsContentIndexed { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this type has an extension table.
    /// </summary>
    public bool HasExtensionTable { get; set; }

    /// <summary>
    /// Gets or sets the extension table name if applicable.
    /// </summary>
    public string? ExtensionTableName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the document type is active.
    /// </summary>
    public bool IsActive { get; set; }
}
