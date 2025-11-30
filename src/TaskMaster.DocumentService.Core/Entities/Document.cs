namespace TaskMaster.DocumentService.Core.Entities;

/// <summary>
/// Represents a document with metadata, versioning, and soft-delete support.
/// </summary>
public class Document
{
    /// <summary>
    /// Gets or sets the unique identifier for the document.
    /// </summary>
    public long Id { get; set; }

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
    /// Gets or sets the metadata as JSON.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the tags as JSON.
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Gets or sets the Meilisearch document identifier for search indexing.
    /// </summary>
    public string? MeilisearchId { get; set; }

    /// <summary>
    /// Gets or sets the last indexing timestamp.
    /// </summary>
    public DateTime? LastIndexedAt { get; set; }

    /// <summary>
    /// Gets or sets the extracted text content for search.
    /// </summary>
    public string? ExtractedText { get; set; }

    /// <summary>
    /// Gets or sets the version number of the document.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Gets or sets the parent document identifier for versioning.
    /// </summary>
    public long? ParentDocumentId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is the current version.
    /// </summary>
    public bool IsCurrentVersion { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user who created the document.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user who last updated the document.
    /// </summary>
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the document is soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the deletion timestamp.
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
    /// Gets or sets the archival timestamp.
    /// </summary>
    public DateTime? ArchivedAt { get; set; }

    // Navigation properties
    /// <summary>
    /// Gets or sets the tenant this document belongs to.
    /// </summary>
    public virtual Tenant Tenant { get; set; } = null!;

    /// <summary>
    /// Gets or sets the document type.
    /// </summary>
    public virtual DocumentType DocumentType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the parent document for versioning.
    /// </summary>
    public virtual Document? ParentDocument { get; set; }

    /// <summary>
    /// Gets or sets the child document versions.
    /// </summary>
    public virtual ICollection<Document> ChildDocuments { get; set; } = new List<Document>();
}
