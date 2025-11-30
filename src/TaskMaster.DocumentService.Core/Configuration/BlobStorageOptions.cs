namespace TaskMaster.DocumentService.Core.Services;

/// <summary>
/// Configuration options for Azure Blob Storage.
/// </summary>
public class BlobStorageOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "BlobStorage";

    /// <summary>
    /// Gets or sets the Azure Storage connection string.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the default container name for documents.
    /// </summary>
    public string DefaultContainerName { get; set; } = "documents";
}
