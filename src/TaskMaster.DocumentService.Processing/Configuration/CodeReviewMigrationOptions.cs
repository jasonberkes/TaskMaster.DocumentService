namespace TaskMaster.DocumentService.Processing.Configuration;

/// <summary>
/// Configuration options for code review migration from TaskMaster.Platform.
/// </summary>
public class CodeReviewMigrationOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the code review migration is enabled.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the source blob container name containing code review documents.
    /// </summary>
    public string SourceContainerName { get; set; } = "platform-code-reviews";

    /// <summary>
    /// Gets or sets the container name for successfully migrated items.
    /// </summary>
    public string MigratedContainerName { get; set; } = "code-reviews-migrated";

    /// <summary>
    /// Gets or sets the container name for failed migration items.
    /// </summary>
    public string FailedMigrationContainerName { get; set; } = "code-reviews-migration-failed";

    /// <summary>
    /// Gets or sets the maximum number of code reviews to process in a single batch.
    /// </summary>
    public int BatchSize { get; set; } = 50;

    /// <summary>
    /// Gets or sets the tenant ID to assign to migrated code reviews.
    /// </summary>
    public int DefaultTenantId { get; set; } = 1;

    /// <summary>
    /// Gets or sets the document type ID for code reviews.
    /// This should match the ID of the 'CodeReview' document type in the database.
    /// </summary>
    public int CodeReviewDocumentTypeId { get; set; } = 2;

    /// <summary>
    /// Gets or sets the system user name for migration operations.
    /// </summary>
    public string SystemUser { get; set; } = "CodeReviewMigration";

    /// <summary>
    /// Gets or sets a value indicating whether to skip already migrated items.
    /// </summary>
    public bool SkipDuplicates { get; set; } = true;

    /// <summary>
    /// Gets or sets the blob metadata key that stores the pull request number.
    /// </summary>
    public string PullRequestNumberMetadataKey { get; set; } = "PullRequestNumber";

    /// <summary>
    /// Gets or sets the blob metadata key that stores code review metadata as JSON.
    /// </summary>
    public string MetadataKey { get; set; } = "CodeReviewMetadata";
}
