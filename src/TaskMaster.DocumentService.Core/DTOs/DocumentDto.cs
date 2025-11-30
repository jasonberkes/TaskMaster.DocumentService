namespace TaskMaster.DocumentService.Core.DTOs;

/// <summary>
/// Data transfer object for document information.
/// </summary>
public class DocumentDto
{
    /// <summary>
    /// Gets or sets the document ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the tenant ID.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the document name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content type.
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the document size in bytes.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Gets or sets the upload timestamp.
    /// </summary>
    public DateTime UploadedAt { get; set; }

    /// <summary>
    /// Gets or sets the uploader user ID.
    /// </summary>
    public string UploadedBy { get; set; } = string.Empty;
}

/// <summary>
/// Data transfer object for creating a document.
/// </summary>
public class CreateDocumentDto
{
    /// <summary>
    /// Gets or sets the document name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content type.
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the document size in bytes.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Gets or sets the blob storage path.
    /// </summary>
    public string BlobPath { get; set; } = string.Empty;
}

/// <summary>
/// Data transfer object for updating a document.
/// </summary>
public class UpdateDocumentDto
{
    /// <summary>
    /// Gets or sets the document name.
    /// </summary>
    public string? Name { get; set; }
}
