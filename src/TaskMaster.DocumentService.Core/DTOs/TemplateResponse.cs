using TaskMaster.DocumentService.Core.Models;

namespace TaskMaster.DocumentService.Core.DTOs;

/// <summary>
/// Response DTO for document template information.
/// </summary>
public class TemplateResponse
{
    /// <summary>
    /// Gets or sets the template identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier.
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
    /// Gets or sets the template content.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template type.
    /// </summary>
    public string TemplateType { get; set; } = "Document";

    /// <summary>
    /// Gets or sets the category.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets the list of variable definitions.
    /// </summary>
    public List<TemplateVariable> Variables { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether the template is public.
    /// </summary>
    public bool IsPublic { get; set; }

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
    /// Gets or sets a value indicating whether the template is active.
    /// </summary>
    public bool IsActive { get; set; }
}
