namespace TaskMaster.DocumentService.Core.Entities;

/// <summary>
/// Represents code review specific metadata as an extension to the Document entity.
/// This table stores additional fields specific to code reviews migrated from TaskMaster.Platform.
/// </summary>
public class CodeReview
{
    /// <summary>
    /// Gets or sets the document identifier. This is a foreign key to the Documents table.
    /// </summary>
    public long DocumentId { get; set; }

    /// <summary>
    /// Gets or sets the pull request number from the source control system.
    /// </summary>
    public int? PullRequestNumber { get; set; }

    /// <summary>
    /// Gets or sets the repository name where the code review originated.
    /// </summary>
    public string? RepositoryName { get; set; }

    /// <summary>
    /// Gets or sets the branch name being reviewed.
    /// </summary>
    public string? BranchName { get; set; }

    /// <summary>
    /// Gets or sets the base branch name (typically main or master).
    /// </summary>
    public string? BaseBranchName { get; set; }

    /// <summary>
    /// Gets or sets the author of the code changes.
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// Gets or sets the reviewers assigned to this code review (stored as JSON array).
    /// </summary>
    public string? Reviewers { get; set; }

    /// <summary>
    /// Gets or sets the status of the code review (e.g., Pending, Approved, Rejected, Merged).
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Gets or sets the URL to the pull request in the source control system.
    /// </summary>
    public string? PullRequestUrl { get; set; }

    /// <summary>
    /// Gets or sets the commit SHA that was reviewed.
    /// </summary>
    public string? CommitSha { get; set; }

    /// <summary>
    /// Gets or sets the date when the code review was submitted.
    /// </summary>
    public DateTime? SubmittedAt { get; set; }

    /// <summary>
    /// Gets or sets the date when the code review was approved.
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// Gets or sets the user who approved the code review.
    /// </summary>
    public string? ApprovedBy { get; set; }

    /// <summary>
    /// Gets or sets the date when the code changes were merged.
    /// </summary>
    public DateTime? MergedAt { get; set; }

    /// <summary>
    /// Gets or sets the user who merged the code changes.
    /// </summary>
    public string? MergedBy { get; set; }

    /// <summary>
    /// Gets or sets the number of files changed in this code review.
    /// </summary>
    public int? FilesChanged { get; set; }

    /// <summary>
    /// Gets or sets the number of lines added.
    /// </summary>
    public int? LinesAdded { get; set; }

    /// <summary>
    /// Gets or sets the number of lines deleted.
    /// </summary>
    public int? LinesDeleted { get; set; }

    /// <summary>
    /// Gets or sets the number of comments made during the review.
    /// </summary>
    public int? CommentCount { get; set; }

    /// <summary>
    /// Gets or sets the original blob path in TaskMaster.Platform storage.
    /// </summary>
    public string? SourceBlobPath { get; set; }

    /// <summary>
    /// Gets or sets the date when this record was migrated.
    /// </summary>
    public DateTime MigratedAt { get; set; }

    /// <summary>
    /// Gets or sets the migration batch identifier for tracking migration runs.
    /// </summary>
    public string? MigrationBatchId { get; set; }

    // Navigation properties
    /// <summary>
    /// Gets or sets the associated document.
    /// </summary>
    public virtual Document Document { get; set; } = null!;
}
