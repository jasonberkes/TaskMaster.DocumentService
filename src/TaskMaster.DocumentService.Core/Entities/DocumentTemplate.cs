namespace TaskMaster.DocumentService.Core.Entities;

/// <summary>
/// Represents a tenant-specific document template with variable substitution support.
/// Templates can be used to generate documents with dynamic content.
/// </summary>
public class DocumentTemplate
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
    /// Gets or sets the MIME type of the template (e.g., text/html, application/vnd.openxmlformats-officedocument.wordprocessingml.document).
    /// </summary>
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the original file name of the template.
    /// </summary>
    public string? OriginalFileName { get; set; }

    /// <summary>
    /// Gets or sets the list of available variables in this template as JSON array.
    /// Example: ["customerName", "invoiceDate", "totalAmount"]
    /// </summary>
    public string? AvailableVariables { get; set; }

    /// <summary>
    /// Gets or sets the template metadata as JSON.
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
    /// Gets or sets a value indicating whether the template is active and available for use.
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

    /// <summary>
    /// Gets or sets the deletion timestamp.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Gets or sets the user who deleted the template.
    /// </summary>
    public string? DeletedBy { get; set; }

    // Navigation properties
    /// <summary>
    /// Gets or sets the tenant this template belongs to.
    /// </summary>
    public virtual Tenant Tenant { get; set; } = null!;

    /// <summary>
    /// Gets or sets the document type.
    /// </summary>
    public virtual DocumentType DocumentType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the parent template for versioning.
    /// </summary>
    public virtual DocumentTemplate? ParentTemplate { get; set; }

    /// <summary>
    /// Gets or sets the child template versions.
    /// </summary>
    public virtual ICollection<DocumentTemplate> ChildTemplates { get; set; } = new List<DocumentTemplate>();
}
