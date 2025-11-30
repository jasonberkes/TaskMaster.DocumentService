namespace TaskMaster.DocumentService.Api.Models;

/// <summary>
/// Request model for uploading a document.
/// </summary>
public class DocumentUploadRequest
{
    /// <summary>
    /// Gets or sets the container name where the document will be stored.
    /// </summary>
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the blob/document.
    /// </summary>
    public string BlobName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the document file to upload.
    /// </summary>
    public IFormFile? File { get; set; }
}
