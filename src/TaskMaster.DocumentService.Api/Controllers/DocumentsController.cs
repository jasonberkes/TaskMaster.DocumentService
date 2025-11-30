using Microsoft.AspNetCore.Mvc;
using TaskMaster.DocumentService.Api.Models;
using TaskMaster.DocumentService.Core.Interfaces;

namespace TaskMaster.DocumentService.Api.Controllers;

/// <summary>
/// Controller for managing document operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DocumentsController : ControllerBase
{
    private readonly IBlobStorageService _blobStorageService;
    private readonly ILogger<DocumentsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentsController"/> class.
    /// </summary>
    /// <param name="blobStorageService">The blob storage service.</param>
    /// <param name="logger">The logger instance.</param>
    public DocumentsController(
        IBlobStorageService blobStorageService,
        ILogger<DocumentsController> logger)
    {
        _blobStorageService = blobStorageService ?? throw new ArgumentNullException(nameof(blobStorageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Uploads a document to blob storage.
    /// </summary>
    /// <param name="request">The upload request containing file and metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The upload result with the document URI.</returns>
    /// <response code="200">Document uploaded successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="500">Internal server error occurred.</response>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(DocumentUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DocumentUploadResponse>> UploadDocument(
        [FromForm] DocumentUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.File == null || request.File.Length == 0)
            {
                _logger.LogWarning("Upload request with no file or empty file");
                return BadRequest(new { error = "File is required and cannot be empty." });
            }

            if (string.IsNullOrWhiteSpace(request.ContainerName))
            {
                _logger.LogWarning("Upload request with no container name");
                return BadRequest(new { error = "Container name is required." });
            }

            if (string.IsNullOrWhiteSpace(request.BlobName))
            {
                _logger.LogWarning("Upload request with no blob name");
                return BadRequest(new { error = "Blob name is required." });
            }

            _logger.LogInformation(
                "Uploading document {BlobName} to container {ContainerName}",
                request.BlobName,
                request.ContainerName);

            using var stream = request.File.OpenReadStream();
            var contentType = request.File.ContentType;

            var uri = await _blobStorageService.UploadAsync(
                request.ContainerName,
                request.BlobName,
                stream,
                contentType,
                cancellationToken);

            var response = new DocumentUploadResponse
            {
                Uri = uri,
                ContainerName = request.ContainerName,
                BlobName = request.BlobName,
                UploadedAt = DateTime.UtcNow
            };

            _logger.LogInformation(
                "Successfully uploaded document {BlobName} to {Uri}",
                request.BlobName,
                uri);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid argument in upload request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document {BlobName}", request.BlobName);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while uploading the document." });
        }
    }

    /// <summary>
    /// Downloads a document from blob storage.
    /// </summary>
    /// <param name="containerName">The container name.</param>
    /// <param name="blobName">The blob name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The document file stream.</returns>
    /// <response code="200">Document downloaded successfully.</response>
    /// <response code="404">Document not found.</response>
    /// <response code="500">Internal server error occurred.</response>
    [HttpGet("download/{containerName}/{blobName}")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DownloadDocument(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Downloading document {BlobName} from container {ContainerName}",
                blobName,
                containerName);

            var stream = await _blobStorageService.DownloadAsync(
                containerName,
                blobName,
                cancellationToken);

            return File(stream, "application/octet-stream", blobName);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogWarning(ex, "Document {BlobName} not found", blobName);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading document {BlobName}", blobName);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while downloading the document." });
        }
    }

    /// <summary>
    /// Deletes a document from blob storage.
    /// </summary>
    /// <param name="containerName">The container name.</param>
    /// <param name="blobName">The blob name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The deletion result.</returns>
    /// <response code="204">Document deleted successfully.</response>
    /// <response code="404">Document not found.</response>
    /// <response code="500">Internal server error occurred.</response>
    [HttpDelete("{containerName}/{blobName}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteDocument(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Deleting document {BlobName} from container {ContainerName}",
                blobName,
                containerName);

            var deleted = await _blobStorageService.DeleteAsync(
                containerName,
                blobName,
                cancellationToken);

            if (!deleted)
            {
                _logger.LogWarning("Document {BlobName} not found for deletion", blobName);
                return NotFound(new { error = $"Document '{blobName}' not found." });
            }

            _logger.LogInformation("Successfully deleted document {BlobName}", blobName);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {BlobName}", blobName);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while deleting the document." });
        }
    }

    /// <summary>
    /// Checks if a document exists in blob storage.
    /// </summary>
    /// <param name="containerName">The container name.</param>
    /// <param name="blobName">The blob name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Information about the document existence.</returns>
    /// <response code="200">Document existence check completed.</response>
    /// <response code="500">Internal server error occurred.</response>
    [HttpGet("exists/{containerName}/{blobName}")]
    [ProducesResponseType(typeof(DocumentInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DocumentInfoResponse>> CheckDocumentExists(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Checking existence of document {BlobName} in container {ContainerName}",
                blobName,
                containerName);

            var exists = await _blobStorageService.ExistsAsync(
                containerName,
                blobName,
                cancellationToken);

            var response = new DocumentInfoResponse
            {
                BlobName = blobName,
                Exists = exists
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence of document {BlobName}", blobName);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while checking document existence." });
        }
    }

    /// <summary>
    /// Generates a SAS URI for temporary access to a document.
    /// </summary>
    /// <param name="request">The SAS URI request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The SAS URI.</returns>
    /// <response code="200">SAS URI generated successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="404">Document not found.</response>
    /// <response code="500">Internal server error occurred.</response>
    [HttpPost("sas")]
    [ProducesResponseType(typeof(SasUriResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SasUriResponse>> GenerateSasUri(
        [FromBody] SasUriRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.ContainerName))
            {
                return BadRequest(new { error = "Container name is required." });
            }

            if (string.IsNullOrWhiteSpace(request.BlobName))
            {
                return BadRequest(new { error = "Blob name is required." });
            }

            if (request.ExpiresInHours <= 0)
            {
                return BadRequest(new { error = "Expiration duration must be greater than zero." });
            }

            _logger.LogInformation(
                "Generating SAS URI for document {BlobName} in container {ContainerName}",
                request.BlobName,
                request.ContainerName);

            var expiresIn = TimeSpan.FromHours(request.ExpiresInHours);
            var sasUri = await _blobStorageService.GetSasUriAsync(
                request.ContainerName,
                request.BlobName,
                expiresIn,
                cancellationToken);

            var response = new SasUriResponse
            {
                SasUri = sasUri,
                ExpiresAt = DateTime.UtcNow.Add(expiresIn)
            };

            _logger.LogInformation(
                "Successfully generated SAS URI for document {BlobName}",
                request.BlobName);

            return Ok(response);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogWarning(ex, "Document {BlobName} not found", request.BlobName);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating SAS URI for document {BlobName}", request.BlobName);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while generating the SAS URI." });
        }
    }

    /// <summary>
    /// Lists all documents in a container.
    /// </summary>
    /// <param name="containerName">The container name.</param>
    /// <param name="prefix">Optional prefix to filter documents.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of document names.</returns>
    /// <response code="200">Documents listed successfully.</response>
    /// <response code="500">Internal server error occurred.</response>
    [HttpGet("list/{containerName}")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<string>>> ListDocuments(
        string containerName,
        [FromQuery] string? prefix = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Listing documents in container {ContainerName} with prefix {Prefix}",
                containerName,
                prefix ?? "(none)");

            var documents = await _blobStorageService.ListBlobsAsync(
                containerName,
                prefix,
                cancellationToken);

            return Ok(documents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing documents in container {ContainerName}", containerName);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while listing documents." });
        }
    }
}
