namespace TaskMaster.DocumentService.Search.Configuration;

/// <summary>
/// Configuration settings for Meilisearch integration.
/// </summary>
public class MeilisearchSettings
{
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
    /// Gets or sets the timeout in seconds for search operations.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the maximum batch size for indexing operations.
    /// </summary>
    public int BatchSize { get; set; } = 100;
}
