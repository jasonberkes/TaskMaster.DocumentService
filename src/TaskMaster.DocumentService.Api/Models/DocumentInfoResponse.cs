namespace TaskMaster.DocumentService.Api.Models;

/// <summary>
/// Response model for document information.
/// </summary>
public class DocumentInfoResponse
{
    /// <summary>
    /// Gets or sets the blob name.
    /// </summary>
    public string BlobName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the document exists.
    /// </summary>
    public bool Exists { get; set; }
}
