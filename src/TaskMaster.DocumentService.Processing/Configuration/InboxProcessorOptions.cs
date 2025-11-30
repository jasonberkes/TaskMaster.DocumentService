namespace TaskMaster.DocumentService.Processing.Configuration;

/// <summary>
/// Configuration options for the inbox processor background service.
/// </summary>
public class InboxProcessorOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "InboxProcessor";

    /// <summary>
    /// Gets or sets a value indicating whether the inbox processor is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the polling interval in seconds.
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the batch size for processing documents.
    /// </summary>
    public int BatchSize { get; set; } = 10;

    /// <summary>
    /// Gets or sets the maximum retry attempts for failed documents.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets a value indicating whether to index documents in search after processing.
    /// </summary>
    public bool EnableSearchIndexing { get; set; } = true;
}
