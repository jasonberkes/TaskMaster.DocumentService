namespace TaskMaster.DocumentService.Core.Entities;

/// <summary>
/// Represents a document in the document service.
/// </summary>
public class Document
{
    /// <summary>
    /// Gets or sets the unique identifier for the document.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier that owns this document.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the document name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the document content type (MIME type).
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the document size in bytes.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Gets or sets the blob storage path or URL.
    /// </summary>
    public string BlobPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the document was uploaded.
    /// </summary>
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the user ID who uploaded the document.
    /// </summary>
    public string UploadedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the document is deleted (soft delete).
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Gets or sets the tenant that owns this document.
    /// </summary>
    public Tenant Tenant { get; set; } = null!;
}
