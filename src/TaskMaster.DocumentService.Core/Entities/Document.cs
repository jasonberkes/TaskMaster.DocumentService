namespace TaskMaster.DocumentService.Core.Entities;

/// <summary>
/// Represents a document in the document management system.
/// </summary>
public class Document
{
    /// <summary>
    /// Gets or sets the unique identifier for the document.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier that owns this document.
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
    /// Gets or sets the blob storage path for the document.
    /// </summary>
    public string BlobPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content hash for deduplication and integrity checking.
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
    /// Gets or sets the metadata as JSON.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the tags as JSON array.
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Gets or sets the Meilisearch document identifier for search indexing.
    /// </summary>
    public string? MeilisearchId { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the document was last indexed in Meilisearch.
    /// </summary>
    public DateTime? LastIndexedAt { get; set; }

    /// <summary>
    /// Gets or sets the extracted text content for search indexing.
    /// </summary>
    public string? ExtractedText { get; set; }

    /// <summary>
    /// Gets or sets the version number of the document.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Gets or sets the parent document identifier for version tracking.
    /// </summary>
    public long? ParentDocumentId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is the current version.
    /// </summary>
    public bool IsCurrentVersion { get; set; } = true;

    /// <summary>
    /// Gets or sets the date and time when the document was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user who created the document.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the document was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user who last updated the document.
    /// </summary>
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the document is deleted (soft delete).
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the document was deleted.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Gets or sets the user who deleted the document.
    /// </summary>
    public string? DeletedBy { get; set; }

    /// <summary>
    /// Gets or sets the reason for deletion.
    /// </summary>
    public string? DeletedReason { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the document is archived.
    /// </summary>
    public bool IsArchived { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the document was archived.
    /// </summary>
    public DateTime? ArchivedAt { get; set; }

    /// <summary>
    /// Gets or sets the tenant navigation property.
    /// </summary>
    public virtual Tenant Tenant { get; set; } = null!;

    /// <summary>
    /// Gets or sets the document type navigation property.
    /// </summary>
    public virtual DocumentType DocumentType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the parent document navigation property.
    /// </summary>
    public virtual Document? ParentDocument { get; set; }

    /// <summary>
    /// Gets or sets the collection of child document versions.
    /// </summary>
    public virtual ICollection<Document> ChildVersions { get; set; } = new List<Document>();
}
