namespace TaskMaster.DocumentService.Search.Configuration;

/// <summary>
/// Configuration options for Meilisearch integration.
/// </summary>
public class MeilisearchOptions
{
    /// <summary>
    /// Gets the configuration section name.
    /// </summary>
    public const string SectionName = "Meilisearch";

    /// <summary>
    /// Gets or sets the Meilisearch server URL.
    /// </summary>
    public string Url { get; set; } = "http://localhost:7700";

    /// <summary>
    /// Gets or sets the Meilisearch API key.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the index name for documents.
    /// </summary>
    public string IndexName { get; set; } = "documents";

    /// <summary>
    /// Gets or sets the batch size for indexing operations.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the timeout in seconds for search operations.
    /// </summary>
    public int SearchTimeoutSeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets whether background indexing is enabled.
    /// When enabled, a background service will periodically index documents that were missed or failed.
    /// </summary>
    public bool BackgroundIndexingEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the interval in minutes between background indexing cycles.
    /// </summary>
    public int BackgroundIndexingIntervalMinutes { get; set; } = 5;
}
