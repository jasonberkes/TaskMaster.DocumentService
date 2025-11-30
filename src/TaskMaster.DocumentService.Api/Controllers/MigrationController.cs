using Microsoft.AspNetCore.Mvc;
using TaskMaster.DocumentService.Core.DTOs;
using TaskMaster.DocumentService.Core.Interfaces;

namespace TaskMaster.DocumentService.Api.Controllers;

/// <summary>
/// Controller for migrating data from TaskMaster.Platform to Document Service.
/// WI #2298: Document Service: Migrate Existing Code Reviews from TaskMaster.Platform
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class MigrationController : ControllerBase
{
    private readonly ICodeReviewMigrationService _migrationService;
    private readonly ILogger<MigrationController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationController"/> class.
    /// </summary>
    /// <param name="migrationService">The migration service.</param>
    /// <param name="logger">The logger.</param>
    public MigrationController(
        ICodeReviewMigrationService migrationService,
        ILogger<MigrationController> logger)
    {
        _migrationService = migrationService ?? throw new ArgumentNullException(nameof(migrationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Migrates code reviews from TaskMaster.Platform to Document Service.
    /// </summary>
    /// <param name="request">The migration request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The migration result.</returns>
    /// <response code="200">Returns the migration result with statistics.</response>
    /// <response code="400">If the request is invalid.</response>
    /// <response code="500">If an internal error occurs during migration.</response>
    [HttpPost("code-reviews")]
    [ProducesResponseType(typeof(MigrateCodeReviewsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MigrateCodeReviewsResponse>> MigrateCodeReviews(
        [FromBody] MigrateCodeReviewsRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            _logger.LogWarning("Received null migration request");
            return BadRequest("Migration request cannot be null.");
        }

        if (request.CodeReviews == null || !request.CodeReviews.Any())
        {
            _logger.LogWarning("Received migration request with no code reviews");
            return BadRequest("No code reviews provided for migration.");
        }

        if (request.TenantId <= 0)
        {
            _logger.LogWarning("Received migration request with invalid tenant ID: {TenantId}", request.TenantId);
            return BadRequest("Invalid tenant ID.");
        }

        try
        {
            _logger.LogInformation(
                "Processing code review migration request. TenantId: {TenantId}, Count: {Count}",
                request.TenantId,
                request.CodeReviews.Count);

            var response = await _migrationService.MigrateCodeReviewsAsync(request, cancellationToken);

            if (response.FailedCount > 0)
            {
                _logger.LogWarning(
                    "Migration completed with errors. Migrated: {Migrated}, Failed: {Failed}",
                    response.MigratedCount,
                    response.FailedCount);
            }
            else
            {
                _logger.LogInformation(
                    "Migration completed successfully. Migrated: {Migrated}, Skipped: {Skipped}",
                    response.MigratedCount,
                    response.SkippedCount);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing code review migration");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { error = "An error occurred during migration.", details = ex.Message });
        }
    }

    /// <summary>
    /// Validates that a tenant exists and is properly configured for migration.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A validation result.</returns>
    /// <response code="200">Returns the validation result.</response>
    /// <response code="400">If the tenant ID is invalid.</response>
    [HttpGet("validate-tenant/{tenantId}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> ValidateTenant(
        int tenantId,
        CancellationToken cancellationToken)
    {
        if (tenantId <= 0)
        {
            return BadRequest(new { isValid = false, message = "Invalid tenant ID." });
        }

        try
        {
            var isValid = await _migrationService.ValidateTenantAsync(tenantId, cancellationToken);

            return Ok(new
            {
                isValid,
                tenantId,
                message = isValid
                    ? "Tenant is valid and ready for migration."
                    : "Tenant does not exist or is not active."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating tenant {TenantId}", tenantId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { error = "An error occurred during validation.", details = ex.Message });
        }
    }

    /// <summary>
    /// Ensures the CodeReview document type exists in the system.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The document type ID.</returns>
    /// <response code="200">Returns the document type ID.</response>
    /// <response code="500">If an error occurs while ensuring the document type exists.</response>
    [HttpPost("ensure-code-review-type")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<object>> EnsureCodeReviewDocumentType(
        CancellationToken cancellationToken)
    {
        try
        {
            var documentTypeId = await _migrationService.EnsureCodeReviewDocumentTypeAsync(cancellationToken);

            _logger.LogInformation("CodeReview document type ensured with ID {DocumentTypeId}", documentTypeId);

            return Ok(new
            {
                documentTypeId,
                documentTypeName = "CodeReview",
                message = "CodeReview document type is ready."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring CodeReview document type");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while ensuring document type exists.", details = ex.Message });
        }
    }
}
