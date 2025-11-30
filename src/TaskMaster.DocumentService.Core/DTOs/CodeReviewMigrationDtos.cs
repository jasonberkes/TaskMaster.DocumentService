namespace TaskMaster.DocumentService.Core.DTOs;

/// <summary>
/// DTO for migrating code reviews from TaskMaster.Platform to Document Service.
/// WI #2298: Document Service: Migrate Existing Code Reviews from TaskMaster.Platform
/// </summary>
public class CodeReviewMigrationDto
{
    /// <summary>
    /// Gets or sets the code review identifier from the source system.
    /// </summary>
    public int SourceCodeReviewId { get; set; }

    /// <summary>
    /// Gets or sets the pull request number.
    /// </summary>
    public int PrNumber { get; set; }

    /// <summary>
    /// Gets or sets the GitHub repository owner (organization or user).
    /// </summary>
    public string? GitHubRepoOwner { get; set; }

    /// <summary>
    /// Gets or sets the GitHub repository name.
    /// </summary>
    public string? GitHubRepoName { get; set; }

    /// <summary>
    /// Gets or sets the associated work item identifier.
    /// </summary>
    public int WorkItemId { get; set; }

    /// <summary>
    /// Gets or sets when the code review was performed.
    /// </summary>
    public DateTime ReviewedAt { get; set; }

    /// <summary>
    /// Gets or sets the overall quality score (0-100).
    /// </summary>
    public int QualityScore { get; set; }

    /// <summary>
    /// Gets or sets the test execution status.
    /// </summary>
    public string TestStatus { get; set; } = "not_run";

    /// <summary>
    /// Gets or sets the build status.
    /// </summary>
    public string BuildStatus { get; set; } = "not_run";

    /// <summary>
    /// Gets or sets whether breaking changes were detected.
    /// </summary>
    public bool BreakingChanges { get; set; }

    /// <summary>
    /// Gets or sets the review recommendation.
    /// </summary>
    public string Recommendation { get; set; } = "APPROVE";

    /// <summary>
    /// Gets or sets the positive findings as JSON array.
    /// </summary>
    public string? PositiveFindings { get; set; }

    /// <summary>
    /// Gets or sets the potential concerns as JSON array.
    /// </summary>
    public string? PotentialConcerns { get; set; }

    /// <summary>
    /// Gets or sets the improvement suggestions as JSON array.
    /// </summary>
    public string? Suggestions { get; set; }

    /// <summary>
    /// Gets or sets the security issues detected as JSON array.
    /// </summary>
    public string? SecurityIssues { get; set; }

    /// <summary>
    /// Gets or sets the performance issues detected as JSON array.
    /// </summary>
    public string? PerformanceIssues { get; set; }

    /// <summary>
    /// Gets or sets the full review response from Claude API.
    /// </summary>
    public string? FullReviewResponse { get; set; }

    /// <summary>
    /// Gets or sets the number of input tokens used.
    /// </summary>
    public int? InputTokens { get; set; }

    /// <summary>
    /// Gets or sets the number of output tokens generated.
    /// </summary>
    public int? OutputTokens { get; set; }

    /// <summary>
    /// Gets or sets the total API cost in USD.
    /// </summary>
    public decimal? ApiCost { get; set; }

    /// <summary>
    /// Gets or sets the Azure Blob Storage URL.
    /// </summary>
    public string? BlobUrl { get; set; }

    /// <summary>
    /// Gets or sets the blob storage path.
    /// </summary>
    public string? BlobPath { get; set; }

    /// <summary>
    /// Gets or sets when the record was created in the source system.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request DTO for migrating code reviews.
/// </summary>
public class MigrateCodeReviewsRequest
{
    /// <summary>
    /// Gets or sets the tenant identifier in the Document Service.
    /// </summary>
    public int TenantId { get; set; }

    /// <summary>
    /// Gets or sets the batch size for processing.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the starting code review ID for pagination.
    /// </summary>
    public int? StartFromId { get; set; }

    /// <summary>
    /// Gets or sets whether to skip already migrated items.
    /// </summary>
    public bool SkipExisting { get; set; } = true;

    /// <summary>
    /// Gets or sets the list of code reviews to migrate (optional, migrates all if null).
    /// </summary>
    public List<CodeReviewMigrationDto>? CodeReviews { get; set; }
}

/// <summary>
/// Response DTO for code review migration.
/// </summary>
public class MigrateCodeReviewsResponse
{
    /// <summary>
    /// Gets or sets the number of code reviews successfully migrated.
    /// </summary>
    public int MigratedCount { get; set; }

    /// <summary>
    /// Gets or sets the number of code reviews skipped.
    /// </summary>
    public int SkippedCount { get; set; }

    /// <summary>
    /// Gets or sets the number of code reviews that failed to migrate.
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// Gets or sets the list of errors encountered during migration.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Gets or sets the timestamp when migration started.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when migration completed.
    /// </summary>
    public DateTime CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the total duration of the migration.
    /// </summary>
    public TimeSpan Duration => CompletedAt - StartedAt;
}
