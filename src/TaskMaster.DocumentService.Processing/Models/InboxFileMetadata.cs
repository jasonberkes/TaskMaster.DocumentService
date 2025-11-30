namespace TaskMaster.DocumentService.Processing.Models;

/// <summary>
/// Represents metadata for a file in the inbox container.
/// Metadata can be provided in the blob name or as blob metadata tags.
/// </summary>
public class InboxFileMetadata
{
    /// <summary>
    /// Gets or sets the blob name.
    /// </summary>
    public string BlobName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    public int TenantId { get; set; }

    /// <summary>
    /// Gets or sets the document type identifier.
    /// </summary>
    public int DocumentTypeId { get; set; }

    /// <summary>
    /// Gets or sets the document title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the document description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the original file name.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content type (MIME type).
    /// </summary>
    public string ContentType { get; set; } = "application/octet-stream";

    /// <summary>
    /// Gets or sets optional metadata as JSON string.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets optional tags as JSON string.
    /// </summary>
    public string? Tags { get; set; }
}
