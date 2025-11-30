namespace TaskMaster.DocumentService.SDK.DTOs;

/// <summary>
/// Request object for creating a new document.
/// </summary>
public class CreateDocumentRequest
{
    /// <summary>
    /// Gets or sets the tenant identifier this document belongs to.
    /// </summary>
    public int TenantId { get; set; }

    /// <summary>
    /// Gets or sets the document type identifier.
    /// </summary>
    public int DocumentTypeId { get; set; }

    /// <summary>
    /// Gets or sets the title of the document.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the document.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the blob storage path for the document content.
    /// </summary>
    public string BlobPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content hash for deduplication.
    /// </summary>
    public string? ContentHash { get; set; }

    /// <summary>
    /// Gets or sets the file size in bytes.
    /// </summary>
    public long? FileSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the MIME type of the document.
    /// </summary>
    public string? MimeType { get; set; }

    /// <summary>
    /// Gets or sets the original file name.
    /// </summary>
    public string? OriginalFileName { get; set; }

    /// <summary>
    /// Gets or sets the metadata as JSON string.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the tags as JSON string.
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Gets or sets the user creating the document.
    /// </summary>
    public string? CreatedBy { get; set; }
}
