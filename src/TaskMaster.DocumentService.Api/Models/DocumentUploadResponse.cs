namespace TaskMaster.DocumentService.Api.Models;

/// <summary>
/// Response model for document upload operations.
/// </summary>
public class DocumentUploadResponse
{
    /// <summary>
    /// Gets or sets the URI of the uploaded document.
    /// </summary>
    public string Uri { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the container name.
    /// </summary>
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the blob name.
    /// </summary>
    public string BlobName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the upload timestamp.
    /// </summary>
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
