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
    /// Gets or sets the inbox container name where files are dropped.
    /// </summary>
    public string InboxContainerName { get; set; } = "inbox";

    /// <summary>
    /// Gets or sets the polling interval in seconds for checking new files.
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the maximum number of files to process in a single batch.
    /// </summary>
    public int BatchSize { get; set; } = 10;

    /// <summary>
    /// Gets or sets whether the processor is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the default tenant ID for documents without tenant metadata.
    /// </summary>
    public int DefaultTenantId { get; set; } = 1;

    /// <summary>
    /// Gets or sets the default document type ID for documents without type metadata.
    /// </summary>
    public int DefaultDocumentTypeId { get; set; } = 1;

    /// <summary>
    /// Gets or sets the processed files container name where files are moved after processing.
    /// </summary>
    public string ProcessedContainerName { get; set; } = "processed";

    /// <summary>
    /// Gets or sets the failed files container name where files are moved on processing errors.
    /// </summary>
    public string FailedContainerName { get; set; } = "failed";

    /// <summary>
    /// Gets or sets the system user name for document creation.
    /// </summary>
    public string SystemUser { get; set; } = "InboxProcessor";
}
