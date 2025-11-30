namespace TaskMaster.DocumentService.Core.DTOs;

/// <summary>
/// Request DTO for rendering a template with variable substitution.
/// </summary>
public class RenderTemplateRequest
{
    /// <summary>
    /// Gets or sets the template identifier.
    /// </summary>
    public int TemplateId { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    public int TenantId { get; set; }

    /// <summary>
    /// Gets or sets the dictionary of variable values for substitution.
    /// </summary>
    public Dictionary<string, string> Variables { get; set; } = new();

    /// <summary>
    /// Gets or sets the optional document identifier if creating a document.
    /// </summary>
    public long? DocumentId { get; set; }
}
