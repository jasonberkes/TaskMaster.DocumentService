namespace TaskMaster.DocumentService.Core.DTOs;

/// <summary>
/// DTO for creating a new version of an existing document
/// </summary>
public class CreateDocumentVersionDto
{
    /// <summary>
    /// Gets or sets the blob storage path for the new version
    /// </summary>
    public string BlobPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content hash
    /// </summary>
    public string? ContentHash { get; set; }

    /// <summary>
    /// Gets or sets the file size in bytes
    /// </summary>
    public long? FileSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the MIME type
    /// </summary>
    public string? MimeType { get; set; }

    /// <summary>
    /// Gets or sets the original filename
    /// </summary>
    public string? OriginalFileName { get; set; }

    /// <summary>
    /// Gets or sets extracted text content
    /// </summary>
    public string? ExtractedText { get; set; }

    /// <summary>
    /// Gets or sets the user creating the version
    /// </summary>
    public string? CreatedBy { get; set; }
}
