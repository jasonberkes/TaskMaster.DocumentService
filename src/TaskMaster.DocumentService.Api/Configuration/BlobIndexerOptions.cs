namespace TaskMaster.DocumentService.Api.Configuration;

/// <summary>
/// Configuration options for the BlobIndexer background job
/// WI #992: Improve Meilisearch Code Quality and Performance
/// WI #3660: Moved to DocumentService
/// </summary>
public class BlobIndexerOptions
{
    public const string SectionName = "BlobIndexer";

    public int IndexIntervalMinutes { get; set; } = 5;
    public int StartupDelaySeconds { get; set; } = 30;
    public int BatchSize { get; set; } = 20;
    public int MaxFileSizeMB { get; set; } = 5;
    public bool Enabled { get; set; } = true;
    public string IndexName { get; set; } = "content";
}
