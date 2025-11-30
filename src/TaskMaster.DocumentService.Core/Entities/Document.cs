namespace TaskMaster.DocumentService.Core.Entities;

/// <summary>
/// Represents a document with versioning, metadata, and soft delete support
/// </summary>
public class Document
{
    /// <summary>
    /// Gets or sets the unique identifier for the document
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier this document belongs to
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
    /// Gets or sets the blob storage path for the document file
    /// </summary>
    public string BlobPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SHA-256 content hash for duplicate detection
    /// </summary>
    public string? ContentHash { get; set; }

    /// <summary>
    /// Gets or sets the file size in bytes
    /// </summary>
    public long? FileSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the MIME type of the document
    /// </summary>
    public string? MimeType { get; set; }

    /// <summary>
    /// Gets or sets the original filename when uploaded
    /// </summary>
    public string? OriginalFileName { get; set; }

    /// <summary>
    /// Gets or sets custom metadata as JSON
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets tags for categorization as JSON array
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Gets or sets the Meilisearch document identifier for search
    /// </summary>
    public string? MeilisearchId { get; set; }

    /// <summary>
    /// Gets or sets when the document was last indexed
    /// </summary>
    public DateTime? LastIndexedAt { get; set; }

    /// <summary>
    /// Gets or sets extracted text content for search indexing
    /// </summary>
    public string? ExtractedText { get; set; }

    /// <summary>
    /// Gets or sets the version number of this document
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Gets or sets the parent document ID for versioning (null for original)
    /// </summary>
    public long? ParentDocumentId { get; set; }

    /// <summary>
    /// Gets or sets whether this is the current version
    /// </summary>
    public bool IsCurrentVersion { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user who created the document
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the last update timestamp
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user who last updated the document
    /// </summary>
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Gets or sets whether the document is soft deleted
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets when the document was deleted
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Gets or sets who deleted the document
    /// </summary>
    public string? DeletedBy { get; set; }

    /// <summary>
    /// Gets or sets the reason for deletion
    /// </summary>
    public string? DeletedReason { get; set; }

    /// <summary>
    /// Gets or sets whether the document is archived
    /// </summary>
    public bool IsArchived { get; set; }

    /// <summary>
    /// Gets or sets when the document was archived
    /// </summary>
    public DateTime? ArchivedAt { get; set; }

    /// <summary>
    /// Navigation property for tenant
    /// </summary>
    public Tenant Tenant { get; set; } = null!;

    /// <summary>
    /// Navigation property for document type
    /// </summary>
    public DocumentType DocumentType { get; set; } = null!;

    /// <summary>
    /// Navigation property for parent document (for versioning)
    /// </summary>
    public Document? ParentDocument { get; set; }

    /// <summary>
    /// Navigation property for document versions
    /// </summary>
    public ICollection<Document> Versions { get; set; } = new List<Document>();
}
