namespace TaskMaster.DocumentService.Search.Models;

/// <summary>
/// Represents a document indexed in Meilisearch for full-text search.
/// </summary>
public class SearchableDocument
{
    /// <summary>
    /// Gets or sets the unique identifier for the searchable document.
    /// This should match the MeilisearchId from the Document entity.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the document identifier from the database.
    /// </summary>
    public long DocumentId { get; set; }

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
    /// Gets or sets the extracted text content from the document.
    /// </summary>
    public string? ExtractedText { get; set; }

    /// <summary>
    /// Gets or sets the original file name.
    /// </summary>
    public string? OriginalFileName { get; set; }

    /// <summary>
    /// Gets or sets the MIME type.
    /// </summary>
    public string? MimeType { get; set; }

    /// <summary>
    /// Gets or sets the tags as a comma-separated string.
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Gets or sets the metadata as a JSON string.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the file size in bytes.
    /// </summary>
    public long? FileSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the version number.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is the current version.
    /// </summary>
    public bool IsCurrentVersion { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the creator.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last updater.
    /// </summary>
    public string? UpdatedBy { get; set; }
}
