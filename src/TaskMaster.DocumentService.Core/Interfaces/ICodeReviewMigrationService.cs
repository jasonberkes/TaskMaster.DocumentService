using TaskMaster.DocumentService.Core.DTOs;

namespace TaskMaster.DocumentService.Core.Interfaces;

/// <summary>
/// Service interface for migrating code reviews from TaskMaster.Platform to Document Service.
/// WI #2298: Document Service: Migrate Existing Code Reviews from TaskMaster.Platform
/// </summary>
public interface ICodeReviewMigrationService
{
    /// <summary>
    /// Migrates code reviews from the source data to the Document Service.
    /// </summary>
    /// <param name="request">The migration request containing parameters and data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A response containing migration results and statistics.</returns>
    Task<MigrateCodeReviewsResponse> MigrateCodeReviewsAsync(
        MigrateCodeReviewsRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that the target tenant exists and is properly configured.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the tenant is valid, false otherwise.</returns>
    Task<bool> ValidateTenantAsync(int tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures the CodeReview document type exists in the system.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The DocumentType identifier for CodeReview.</returns>
    Task<int> EnsureCodeReviewDocumentTypeAsync(CancellationToken cancellationToken = default);
}
