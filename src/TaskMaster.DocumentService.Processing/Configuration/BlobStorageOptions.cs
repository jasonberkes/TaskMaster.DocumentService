namespace TaskMaster.DocumentService.Processing.Configuration;

/// <summary>
/// Configuration options for blob storage.
/// </summary>
public class BlobStorageOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "BlobStorage";

    /// <summary>
    /// Gets or sets the connection string for Azure Blob Storage.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the inbox container name where new documents are dropped.
    /// </summary>
    public string InboxContainerName { get; set; } = "inbox";

    /// <summary>
    /// Gets or sets the processed container name where successfully processed documents are moved.
    /// </summary>
    public string ProcessedContainerName { get; set; } = "processed";

    /// <summary>
    /// Gets or sets the failed container name where failed documents are moved.
    /// </summary>
    public string FailedContainerName { get; set; } = "failed";

    /// <summary>
    /// Gets or sets the main documents container name.
    /// </summary>
    public string DocumentsContainerName { get; set; } = "documents";

    /// <summary>
    /// Gets or sets the default tenant ID to use when not specified in blob metadata.
    /// </summary>
    public int DefaultTenantId { get; set; } = 1;

    /// <summary>
    /// Gets or sets the default document type ID to use when not specified in blob metadata.
    /// </summary>
    public int DefaultDocumentTypeId { get; set; } = 1;
}
