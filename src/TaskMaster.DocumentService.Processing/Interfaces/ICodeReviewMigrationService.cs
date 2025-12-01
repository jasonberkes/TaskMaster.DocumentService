namespace TaskMaster.DocumentService.Processing.Interfaces;

/// <summary>
/// Service interface for migrating code reviews from TaskMaster.Platform to the Document Service.
/// </summary>
public interface ICodeReviewMigrationService
{
    /// <summary>
    /// Migrates code review documents from the source blob container.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of code reviews successfully migrated.</returns>
    Task<int> MigrateCodeReviewsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of a migration batch.
    /// </summary>
    /// <param name="batchId">The migration batch identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Migration batch statistics.</returns>
    Task<MigrationBatchStatus> GetMigrationStatusAsync(string batchId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the status of a migration batch.
/// </summary>
public class MigrationBatchStatus
{
    /// <summary>
    /// Gets or sets the migration batch identifier.
    /// </summary>
    public string BatchId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total number of items processed.
    /// </summary>
    public int TotalProcessed { get; set; }

    /// <summary>
    /// Gets or sets the number of successfully migrated items.
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Gets or sets the number of failed items.
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Gets or sets the migration start time.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the migration completion time.
    /// </summary>
    public DateTime? CompletedAt { get; set; }
}
