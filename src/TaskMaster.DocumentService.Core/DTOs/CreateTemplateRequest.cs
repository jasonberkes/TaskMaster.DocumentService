using TaskMaster.DocumentService.Core.Models;

namespace TaskMaster.DocumentService.Core.DTOs;

/// <summary>
/// Request DTO for creating a new document template.
/// </summary>
public class CreateTemplateRequest
{
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
    /// Gets or sets the template content with variable placeholders.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template type (e.g., Document, Email, Report).
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
}
