namespace TaskMaster.DocumentService.Core.Models;

/// <summary>
/// Represents a document in the inbox ready for processing.
/// </summary>
public class InboxDocument
{
    /// <summary>
    /// Gets or sets the blob name (filename).
    /// </summary>
    public string BlobName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the full blob URI.
    /// </summary>
    public Uri? BlobUri { get; set; }

    /// <summary>
    /// Gets or sets the content stream.
    /// </summary>
    public Stream? ContentStream { get; set; }

    /// <summary>
    /// Gets or sets the content type.
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Gets or sets the content length in bytes.
    /// </summary>
    public long ContentLength { get; set; }

    /// <summary>
    /// Gets or sets the blob creation time.
    /// </summary>
    public DateTimeOffset? CreatedOn { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier extracted from metadata or blob path.
    /// </summary>
    public int TenantId { get; set; }

    /// <summary>
    /// Gets or sets the document type identifier extracted from metadata or blob path.
    /// </summary>
    public int DocumentTypeId { get; set; }
}
