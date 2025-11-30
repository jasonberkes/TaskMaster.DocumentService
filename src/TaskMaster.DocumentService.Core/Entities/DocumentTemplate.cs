namespace TaskMaster.DocumentService.Core.Entities;

/// <summary>
/// Represents a document template for creating documents from tenant-specific templates with variable substitution.
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
    /// Gets or sets the template name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the template content with variable placeholders (e.g., {{variableName}}).
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template type (e.g., Document, Email, Report).
    /// </summary>
    public string TemplateType { get; set; } = "Document";

    /// <summary>
    /// Gets or sets the category for organizing templates.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets the JSON array of variable definitions including name, type, default value, and validation rules.
    /// </summary>
    public string? Variables { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the template is public (shared across tenant hierarchy).
    /// </summary>
    public bool IsPublic { get; set; } = false;

    /// <summary>
    /// Gets or sets the version number of the template.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Gets or sets a value indicating whether this is the current version of the template.
    /// </summary>
    public bool IsCurrentVersion { get; set; } = true;

    /// <summary>
    /// Gets or sets the parent template identifier for versioning.
    /// </summary>
    public int? ParentTemplateId { get; set; }

    /// <summary>
    /// Gets or sets the parent template navigation property.
    /// </summary>
    public DocumentTemplate? ParentTemplate { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

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
    /// Gets or sets a value indicating whether the template is soft deleted.
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Gets or sets the deletion timestamp.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Gets or sets the user who deleted the template.
    /// </summary>
    public string? DeletedBy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the template is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
