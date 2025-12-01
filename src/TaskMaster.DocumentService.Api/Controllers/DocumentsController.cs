using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskMaster.DocumentService.Api.Authorization;
using TaskMaster.DocumentService.Api.Extensions;
using TaskMaster.DocumentService.Core.Interfaces;

namespace TaskMaster.DocumentService.Api.Controllers;

/// <summary>
/// Controller for document management operations including CRUD, versioning, and download operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // Requires either JWT or API Key authentication
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly ILogger<DocumentsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentsController"/> class.
    /// </summary>
    /// <param name="documentService">The document service.</param>
    /// <param name="logger">The logger.</param>
    public DocumentsController(
        IDocumentService documentService,
        ILogger<DocumentsController> logger)
    {
        _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new document with file upload.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="documentTypeId">The document type identifier.</param>
    /// <param name="title">The document title.</param>
    /// <param name="description">Optional document description.</param>
    /// <param name="file">The file to upload.</param>
    /// <param name="metadata">Optional metadata as JSON string.</param>
    /// <param name="tags">Optional tags as JSON string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created document.</returns>
    /// <response code="201">Returns the newly created document.</response>
    /// <response code="400">If the request is invalid.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user does not have access to the specified tenant.</response>
    [HttpPost]
    [TenantAuthorization("tenantId")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [RequestSizeLimit(524288000)] // 500 MB limit
    public async Task<IActionResult> CreateDocument(
        [FromForm] int tenantId,
        [FromForm] int documentTypeId,
        [FromForm] string title,
        [FromForm] string? description,
        [FromForm] IFormFile file,
        [FromForm] string? metadata,
        [FromForm] string? tags,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "InvalidFile", message = "File is required and cannot be empty." });
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                return BadRequest(new { error = "InvalidTitle", message = "Title is required." });
            }

            var createdBy = User.Identity?.Name ?? "system";

            _logger.LogInformation(
                "Creating document '{Title}' for tenant {TenantId} by user {CreatedBy}",
                title, tenantId, createdBy);

            using var stream = file.OpenReadStream();
            var document = await _documentService.CreateDocumentAsync(
                tenantId,
                documentTypeId,
                title,
                description,
                stream,
                file.FileName,
                file.ContentType,
                metadata,
                tags,
                createdBy,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetDocumentById),
                new { documentId = document.Id },
                new
                {
                    id = document.Id,
                    tenantId = document.TenantId,
                    documentTypeId = document.DocumentTypeId,
                    title = document.Title,
                    description = document.Description,
                    originalFileName = document.OriginalFileName,
                    mimeType = document.MimeType,
                    fileSizeBytes = document.FileSizeBytes,
                    contentHash = document.ContentHash,
                    metadata = document.Metadata,
                    tags = document.Tags,
                    version = document.Version,
                    isCurrentVersion = document.IsCurrentVersion,
                    createdAt = document.CreatedAt,
                    createdBy = document.CreatedBy
                });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when creating document");
            return BadRequest(new { error = "InvalidOperation", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create document");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while creating the document." });
        }
    }

    /// <summary>
    /// Gets a document by its identifier.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The document information.</returns>
    /// <response code="200">Returns the document information.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the document is not found.</response>
    [HttpGet("{documentId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDocumentById(long documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var document = await _documentService.GetDocumentByIdAsync(documentId, cancellationToken);

            if (document == null)
            {
                _logger.LogWarning("Document {DocumentId} not found", documentId);
                return NotFound(new { error = "DocumentNotFound", message = $"Document with ID {documentId} not found." });
            }

            // Verify tenant access
            var userTenantId = User.GetTenantId();
            if (document.TenantId != userTenantId)
            {
                _logger.LogWarning(
                    "User from tenant {UserTenantId} attempted to access document from tenant {DocumentTenantId}",
                    userTenantId, document.TenantId);
                return NotFound(new { error = "DocumentNotFound", message = $"Document with ID {documentId} not found." });
            }

            return Ok(new
            {
                id = document.Id,
                tenantId = document.TenantId,
                documentTypeId = document.DocumentTypeId,
                title = document.Title,
                description = document.Description,
                originalFileName = document.OriginalFileName,
                mimeType = document.MimeType,
                fileSizeBytes = document.FileSizeBytes,
                contentHash = document.ContentHash,
                metadata = document.Metadata,
                tags = document.Tags,
                version = document.Version,
                parentDocumentId = document.ParentDocumentId,
                isCurrentVersion = document.IsCurrentVersion,
                isDeleted = document.IsDeleted,
                isArchived = document.IsArchived,
                createdAt = document.CreatedAt,
                createdBy = document.CreatedBy,
                updatedAt = document.UpdatedAt,
                updatedBy = document.UpdatedBy
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve document {DocumentId}", documentId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while retrieving the document." });
        }
    }

    /// <summary>
    /// Gets all documents for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="includeDeleted">Whether to include soft-deleted documents.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of documents.</returns>
    /// <response code="200">Returns the list of documents.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user does not have access to the specified tenant.</response>
    [HttpGet("tenant/{tenantId}")]
    [TenantAuthorization("tenantId")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDocumentsByTenant(
        int tenantId,
        [FromQuery] bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Retrieving documents for tenant {TenantId}, includeDeleted: {IncludeDeleted}",
                tenantId, includeDeleted);

            var documents = await _documentService.GetDocumentsByTenantAsync(tenantId, includeDeleted, cancellationToken);

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
                isDeleted = d.IsDeleted,
                isArchived = d.IsArchived,
                createdAt = d.CreatedAt,
                createdBy = d.CreatedBy
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve documents for tenant {TenantId}", tenantId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while retrieving documents." });
        }
    }

    /// <summary>
    /// Gets documents by document type.
    /// </summary>
    /// <param name="documentTypeId">The document type identifier.</param>
    /// <param name="includeDeleted">Whether to include soft-deleted documents.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of documents.</returns>
    /// <response code="200">Returns the list of documents.</response>
    /// <response code="401">If the user is not authenticated.</response>
    [HttpGet("type/{documentTypeId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetDocumentsByType(
        int documentTypeId,
        [FromQuery] bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Retrieving documents for document type {DocumentTypeId}, includeDeleted: {IncludeDeleted}",
                documentTypeId, includeDeleted);

            var documents = await _documentService.GetDocumentsByTypeAsync(documentTypeId, includeDeleted, cancellationToken);

            // Filter by user's tenant
            var userTenantId = User.GetTenantId();
            var filteredDocuments = documents.Where(d => d.TenantId == userTenantId);

            return Ok(filteredDocuments.Select(d => new
            {
                id = d.Id,
                tenantId = d.TenantId,
                title = d.Title,
                description = d.Description,
                originalFileName = d.OriginalFileName,
                mimeType = d.MimeType,
                fileSizeBytes = d.FileSizeBytes,
                version = d.Version,
                isCurrentVersion = d.IsCurrentVersion,
                isDeleted = d.IsDeleted,
                isArchived = d.IsArchived,
                createdAt = d.CreatedAt,
                createdBy = d.CreatedBy
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve documents for document type {DocumentTypeId}", documentTypeId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while retrieving documents." });
        }
    }

    /// <summary>
    /// Updates document metadata and properties.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="title">The updated title.</param>
    /// <param name="description">The updated description.</param>
    /// <param name="metadata">The updated metadata as JSON string.</param>
    /// <param name="tags">The updated tags as JSON string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated document.</returns>
    /// <response code="200">Returns the updated document.</response>
    /// <response code="400">If the request is invalid.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the document is not found.</response>
    [HttpPut("{documentId}/metadata")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDocumentMetadata(
        long documentId,
        [FromBody] UpdateDocumentMetadataRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // First, verify the document exists and belongs to user's tenant
            var existingDocument = await _documentService.GetDocumentByIdAsync(documentId, cancellationToken);
            if (existingDocument == null)
            {
                return NotFound(new { error = "DocumentNotFound", message = $"Document with ID {documentId} not found." });
            }

            var userTenantId = User.GetTenantId();
            if (existingDocument.TenantId != userTenantId)
            {
                return NotFound(new { error = "DocumentNotFound", message = $"Document with ID {documentId} not found." });
            }

            var updatedBy = User.Identity?.Name ?? "system";

            var document = await _documentService.UpdateDocumentMetadataAsync(
                documentId,
                request.Title,
                request.Description,
                request.Metadata,
                request.Tags,
                updatedBy,
                cancellationToken);

            return Ok(new
            {
                id = document.Id,
                tenantId = document.TenantId,
                documentTypeId = document.DocumentTypeId,
                title = document.Title,
                description = document.Description,
                metadata = document.Metadata,
                tags = document.Tags,
                updatedAt = document.UpdatedAt,
                updatedBy = document.UpdatedBy
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when updating document metadata");
            return BadRequest(new { error = "InvalidOperation", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update document {DocumentId} metadata", documentId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while updating the document." });
        }
    }

    /// <summary>
    /// Creates a new version of an existing document.
    /// </summary>
    /// <param name="documentId">The parent document identifier.</param>
    /// <param name="file">The file for the new version.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new document version.</returns>
    /// <response code="201">Returns the newly created document version.</response>
    /// <response code="400">If the request is invalid.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the parent document is not found.</response>
    [HttpPost("{documentId}/versions")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [RequestSizeLimit(524288000)] // 500 MB limit
    public async Task<IActionResult> CreateDocumentVersion(
        long documentId,
        [FromForm] IFormFile file,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "InvalidFile", message = "File is required and cannot be empty." });
            }

            // Verify the parent document exists and belongs to user's tenant
            var parentDocument = await _documentService.GetDocumentByIdAsync(documentId, cancellationToken);
            if (parentDocument == null)
            {
                return NotFound(new { error = "DocumentNotFound", message = $"Document with ID {documentId} not found." });
            }

            var userTenantId = User.GetTenantId();
            if (parentDocument.TenantId != userTenantId)
            {
                return NotFound(new { error = "DocumentNotFound", message = $"Document with ID {documentId} not found." });
            }

            var updatedBy = User.Identity?.Name ?? "system";

            using var stream = file.OpenReadStream();
            var newVersion = await _documentService.CreateDocumentVersionAsync(
                documentId,
                stream,
                file.FileName,
                file.ContentType,
                updatedBy,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetDocumentById),
                new { documentId = newVersion.Id },
                new
                {
                    id = newVersion.Id,
                    tenantId = newVersion.TenantId,
                    documentTypeId = newVersion.DocumentTypeId,
                    title = newVersion.Title,
                    description = newVersion.Description,
                    originalFileName = newVersion.OriginalFileName,
                    mimeType = newVersion.MimeType,
                    fileSizeBytes = newVersion.FileSizeBytes,
                    contentHash = newVersion.ContentHash,
                    version = newVersion.Version,
                    parentDocumentId = newVersion.ParentDocumentId,
                    isCurrentVersion = newVersion.IsCurrentVersion,
                    createdAt = newVersion.CreatedAt,
                    createdBy = newVersion.CreatedBy
                });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when creating document version");
            return BadRequest(new { error = "InvalidOperation", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create version for document {DocumentId}", documentId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while creating the document version." });
        }
    }

    /// <summary>
    /// Gets all versions of a document.
    /// </summary>
    /// <param name="documentId">The parent document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of document versions.</returns>
    /// <response code="200">Returns the list of document versions.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the document is not found.</response>
    [HttpGet("{documentId}/versions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDocumentVersions(long documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify the document exists and belongs to user's tenant
            var document = await _documentService.GetDocumentByIdAsync(documentId, cancellationToken);
            if (document == null)
            {
                return NotFound(new { error = "DocumentNotFound", message = $"Document with ID {documentId} not found." });
            }

            var userTenantId = User.GetTenantId();
            if (document.TenantId != userTenantId)
            {
                return NotFound(new { error = "DocumentNotFound", message = $"Document with ID {documentId} not found." });
            }

            var versions = await _documentService.GetDocumentVersionsAsync(documentId, cancellationToken);

            return Ok(versions.Select(v => new
            {
                id = v.Id,
                version = v.Version,
                originalFileName = v.OriginalFileName,
                mimeType = v.MimeType,
                fileSizeBytes = v.FileSizeBytes,
                contentHash = v.ContentHash,
                isCurrentVersion = v.IsCurrentVersion,
                createdAt = v.CreatedAt,
                createdBy = v.CreatedBy
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve versions for document {DocumentId}", documentId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while retrieving document versions." });
        }
    }

    /// <summary>
    /// Gets the current version of a document.
    /// </summary>
    /// <param name="documentId">The parent document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current document version.</returns>
    /// <response code="200">Returns the current document version.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the document is not found or has no current version.</response>
    [HttpGet("{documentId}/versions/current")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentVersion(long documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify the document exists and belongs to user's tenant
            var document = await _documentService.GetDocumentByIdAsync(documentId, cancellationToken);
            if (document == null)
            {
                return NotFound(new { error = "DocumentNotFound", message = $"Document with ID {documentId} not found." });
            }

            var userTenantId = User.GetTenantId();
            if (document.TenantId != userTenantId)
            {
                return NotFound(new { error = "DocumentNotFound", message = $"Document with ID {documentId} not found." });
            }

            var currentVersion = await _documentService.GetCurrentVersionAsync(documentId, cancellationToken);
            if (currentVersion == null)
            {
                return NotFound(new { error = "CurrentVersionNotFound", message = $"No current version found for document {documentId}." });
            }

            return Ok(new
            {
                id = currentVersion.Id,
                version = currentVersion.Version,
                title = currentVersion.Title,
                description = currentVersion.Description,
                originalFileName = currentVersion.OriginalFileName,
                mimeType = currentVersion.MimeType,
                fileSizeBytes = currentVersion.FileSizeBytes,
                contentHash = currentVersion.ContentHash,
                isCurrentVersion = currentVersion.IsCurrentVersion,
                createdAt = currentVersion.CreatedAt,
                createdBy = currentVersion.CreatedBy
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve current version for document {DocumentId}", documentId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while retrieving the current document version." });
        }
    }

    /// <summary>
    /// Downloads document content.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The document content as a file.</returns>
    /// <response code="200">Returns the document file.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the document is not found.</response>
    [HttpGet("{documentId}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadDocument(long documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify the document exists and belongs to user's tenant
            var document = await _documentService.GetDocumentByIdAsync(documentId, cancellationToken);
            if (document == null)
            {
                return NotFound(new { error = "DocumentNotFound", message = $"Document with ID {documentId} not found." });
            }

            var userTenantId = User.GetTenantId();
            if (document.TenantId != userTenantId)
            {
                return NotFound(new { error = "DocumentNotFound", message = $"Document with ID {documentId} not found." });
            }

            var stream = await _documentService.DownloadDocumentAsync(documentId, cancellationToken);

            return File(
                stream,
                document.MimeType ?? "application/octet-stream",
                document.OriginalFileName ?? $"document-{documentId}");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when downloading document");
            return BadRequest(new { error = "InvalidOperation", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download document {DocumentId}", documentId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while downloading the document." });
        }
    }

    /// <summary>
    /// Gets a temporary SAS URI for document access.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="expiresInMinutes">The duration in minutes for which the SAS token is valid (default: 60).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The SAS URI for temporary access.</returns>
    /// <response code="200">Returns the SAS URI.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the document is not found.</response>
    [HttpGet("{documentId}/sas")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDocumentSasUri(
        long documentId,
        [FromQuery] int expiresInMinutes = 60,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify the document exists and belongs to user's tenant
            var document = await _documentService.GetDocumentByIdAsync(documentId, cancellationToken);
            if (document == null)
            {
                return NotFound(new { error = "DocumentNotFound", message = $"Document with ID {documentId} not found." });
            }

            var userTenantId = User.GetTenantId();
            if (document.TenantId != userTenantId)
            {
                return NotFound(new { error = "DocumentNotFound", message = $"Document with ID {documentId} not found." });
            }

            var expiresIn = TimeSpan.FromMinutes(Math.Max(1, Math.Min(expiresInMinutes, 1440))); // Limit between 1 minute and 24 hours
            var sasUri = await _documentService.GetDocumentSasUriAsync(documentId, expiresIn, cancellationToken);

            return Ok(new
            {
                sasUri,
                expiresIn = expiresIn.TotalMinutes,
                expiresAt = DateTime.UtcNow.Add(expiresIn)
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when generating SAS URI");
            return BadRequest(new { error = "InvalidOperation", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate SAS URI for document {DocumentId}", documentId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while generating the SAS URI." });
        }
    }

    /// <summary>
    /// Soft-deletes a document.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="deletedReason">Optional reason for deletion.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    /// <response code="204">Document successfully deleted.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the document is not found.</response>
    [HttpDelete("{documentId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDocument(
        long documentId,
        [FromQuery] string? deletedReason = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify the document exists and belongs to user's tenant
            var document = await _documentService.GetDocumentByIdAsync(documentId, cancellationToken);
            if (document == null)
            {
                return NotFound(new { error = "DocumentNotFound", message = $"Document with ID {documentId} not found." });
            }

            var userTenantId = User.GetTenantId();
            if (document.TenantId != userTenantId)
            {
                return NotFound(new { error = "DocumentNotFound", message = $"Document with ID {documentId} not found." });
            }

            var deletedBy = User.Identity?.Name ?? "system";

            await _documentService.DeleteDocumentAsync(documentId, deletedBy, deletedReason, cancellationToken);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when deleting document");
            return BadRequest(new { error = "InvalidOperation", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete document {DocumentId}", documentId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while deleting the document." });
        }
    }

    /// <summary>
    /// Restores a soft-deleted document.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    /// <response code="204">Document successfully restored.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the document is not found.</response>
    [HttpPost("{documentId}/restore")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestoreDocument(long documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify the document exists and belongs to user's tenant (need to check with includeDeleted)
            var userTenantId = User.GetTenantId() ?? 0;
            var documents = await _documentService.GetDocumentsByTenantAsync(userTenantId, true, cancellationToken);
            var document = documents.FirstOrDefault(d => d.Id == documentId);

            if (document == null)
            {
                return NotFound(new { error = "DocumentNotFound", message = $"Document with ID {documentId} not found." });
            }

            await _documentService.RestoreDocumentAsync(documentId, cancellationToken);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when restoring document");
            return BadRequest(new { error = "InvalidOperation", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore document {DocumentId}", documentId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while restoring the document." });
        }
    }

    /// <summary>
    /// Archives a document.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    /// <response code="204">Document successfully archived.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the document is not found.</response>
    [HttpPost("{documentId}/archive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ArchiveDocument(long documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify the document exists and belongs to user's tenant
            var document = await _documentService.GetDocumentByIdAsync(documentId, cancellationToken);
            if (document == null)
            {
                return NotFound(new { error = "DocumentNotFound", message = $"Document with ID {documentId} not found." });
            }

            var userTenantId = User.GetTenantId();
            if (document.TenantId != userTenantId)
            {
                return NotFound(new { error = "DocumentNotFound", message = $"Document with ID {documentId} not found." });
            }

            await _documentService.ArchiveDocumentAsync(documentId, cancellationToken);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when archiving document");
            return BadRequest(new { error = "InvalidOperation", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to archive document {DocumentId}", documentId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while archiving the document." });
        }
    }

    /// <summary>
    /// Gets all archived documents for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of archived documents.</returns>
    /// <response code="200">Returns the list of archived documents.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user does not have access to the specified tenant.</response>
    [HttpGet("tenant/{tenantId}/archived")]
    [TenantAuthorization("tenantId")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetArchivedDocuments(int tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving archived documents for tenant {TenantId}", tenantId);

            var documents = await _documentService.GetArchivedDocumentsAsync(tenantId, cancellationToken);

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
                archivedAt = d.ArchivedAt,
                createdAt = d.CreatedAt,
                createdBy = d.CreatedBy
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve archived documents for tenant {TenantId}", tenantId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while retrieving archived documents." });
        }
    }

    /// <summary>
    /// Finds duplicate documents by content hash.
    /// </summary>
    /// <param name="contentHash">The content hash to search for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of documents with the same content hash.</returns>
    /// <response code="200">Returns the list of duplicate documents.</response>
    /// <response code="400">If the content hash is invalid.</response>
    /// <response code="401">If the user is not authenticated.</response>
    [HttpGet("duplicates/{contentHash}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> FindDuplicateDocuments(string contentHash, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(contentHash))
            {
                return BadRequest(new { error = "InvalidContentHash", message = "Content hash is required." });
            }

            var documents = await _documentService.FindDuplicateDocumentsAsync(contentHash, cancellationToken);

            // Filter by user's tenant
            var userTenantId = User.GetTenantId();
            var filteredDocuments = documents.Where(d => d.TenantId == userTenantId);

            return Ok(filteredDocuments.Select(d => new
            {
                id = d.Id,
                tenantId = d.TenantId,
                documentTypeId = d.DocumentTypeId,
                title = d.Title,
                originalFileName = d.OriginalFileName,
                mimeType = d.MimeType,
                fileSizeBytes = d.FileSizeBytes,
                contentHash = d.ContentHash,
                createdAt = d.CreatedAt,
                createdBy = d.CreatedBy
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find duplicate documents with content hash {ContentHash}", contentHash);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalError", message = "An error occurred while finding duplicate documents." });
        }
    }
}

/// <summary>
/// Request model for updating document metadata.
/// </summary>
public class UpdateDocumentMetadataRequest
{
    /// <summary>
    /// Gets or sets the updated title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the updated description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the updated metadata as JSON string.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the updated tags as JSON string.
    /// </summary>
    public string? Tags { get; set; }
}
