namespace TaskMaster.DocumentService.Core.DTOs;

/// <summary>
/// DTO for returning document information
/// </summary>
public class DocumentDto
{
    /// <summary>
    /// Gets or sets the document identifier
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier
    /// </summary>
    public int TenantId { get; set; }

    /// <summary>
    /// Gets or sets the document type identifier
    /// </summary>
    public int DocumentTypeId { get; set; }

    /// <summary>
    /// Gets or sets the document title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the document description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the blob storage path
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
    /// Gets or sets custom metadata as JSON string
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets tags as JSON array string
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Gets or sets the version number
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Gets or sets the parent document ID for versioning
    /// </summary>
    public long? ParentDocumentId { get; set; }

    /// <summary>
    /// Gets or sets whether this is the current version
    /// </summary>
    public bool IsCurrentVersion { get; set; }

    /// <summary>
    /// Gets or sets whether the document is archived
    /// </summary>
    public bool IsArchived { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the creator username
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the last update timestamp
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last updater username
    /// </summary>
    public string? UpdatedBy { get; set; }
}
