namespace TaskMaster.DocumentService.Core.Entities;

/// <summary>
/// Represents a document template with support for variable substitution.
/// </summary>
public class DocumentTemplate
{
    /// <summary>
    /// Gets or sets the unique identifier for the template.
    /// </summary>
    public int Id { get; set; }

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
    /// Gets or sets the description of the template.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the template content with variable placeholders.
    /// Variables should be in the format {{variableName}}.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MIME type of the template content.
    /// </summary>
    public string MimeType { get; set; } = "text/plain";

    /// <summary>
    /// Gets or sets the file extension for documents created from this template.
    /// </summary>
    public string? FileExtension { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this template is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the default title pattern for documents created from this template.
    /// Can include variable placeholders.
    /// </summary>
    public string? DefaultTitlePattern { get; set; }

    /// <summary>
    /// Gets or sets the default description pattern for documents created from this template.
    /// Can include variable placeholders.
    /// </summary>
    public string? DefaultDescriptionPattern { get; set; }

    /// <summary>
    /// Gets or sets the metadata as JSON.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the tags as JSON.
    /// </summary>
    public string? Tags { get; set; }

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

    /// <summary>
    /// Gets or sets the reason for deletion.
    /// </summary>
    public string? DeletedReason { get; set; }

    // Navigation properties
    /// <summary>
    /// Gets or sets the tenant this template belongs to.
    /// </summary>
    public virtual Tenant Tenant { get; set; } = null!;

    /// <summary>
    /// Gets or sets the document type for this template.
    /// </summary>
    public virtual DocumentType DocumentType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the variables defined in this template.
    /// </summary>
    public virtual ICollection<TemplateVariable> Variables { get; set; } = new List<TemplateVariable>();
}
