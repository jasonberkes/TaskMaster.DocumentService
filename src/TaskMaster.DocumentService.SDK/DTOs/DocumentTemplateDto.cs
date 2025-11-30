namespace TaskMaster.DocumentService.SDK.DTOs;

/// <summary>
/// Data transfer object for DocumentTemplate entity.
/// </summary>
public class DocumentTemplateDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the template.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier this template belongs to.
    /// </summary>
    public int TenantId { get; set; }

    /// <summary>
    /// Gets or sets the document type identifier for documents created from this template.
    /// </summary>
    public int DocumentTypeId { get; set; }

    /// <summary>
    /// Gets or sets the template name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the blob storage path for the template content.
    /// </summary>
    public string BlobPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file size in bytes.
    /// </summary>
    public long? FileSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the MIME type of the template.
    /// </summary>
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the original file name of the template.
    /// </summary>
    public string? OriginalFileName { get; set; }

    /// <summary>
    /// Gets or sets the list of available variables in this template as JSON array.
    /// </summary>
    public string? AvailableVariables { get; set; }

    /// <summary>
    /// Gets or sets the template metadata as JSON string.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the category or tag for organizing templates.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets the version number of the template.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Gets or sets the parent template identifier for versioning.
    /// </summary>
    public long? ParentTemplateId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is the current version.
    /// </summary>
    public bool IsCurrentVersion { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the template is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user who created the template.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user who last updated the template.
    /// </summary>
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the template is soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }
}
