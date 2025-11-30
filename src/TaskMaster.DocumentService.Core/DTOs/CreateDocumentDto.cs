namespace TaskMaster.DocumentService.Core.DTOs;

/// <summary>
/// DTO for creating a new document
/// </summary>
public class CreateDocumentDto
{
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
    /// Gets or sets the content hash for duplicate detection
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
    /// Gets or sets extracted text content
    /// </summary>
    public string? ExtractedText { get; set; }

    /// <summary>
    /// Gets or sets the user creating the document
    /// </summary>
    public string? CreatedBy { get; set; }
}
