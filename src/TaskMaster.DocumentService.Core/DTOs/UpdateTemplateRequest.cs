using TaskMaster.DocumentService.Core.Models;

namespace TaskMaster.DocumentService.Core.DTOs;

/// <summary>
/// Request DTO for updating an existing document template.
/// </summary>
public class UpdateTemplateRequest
{
    /// <summary>
    /// Gets or sets the template identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the template name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the template content with variable placeholders.
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
    public bool IsPublic { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether the template is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
