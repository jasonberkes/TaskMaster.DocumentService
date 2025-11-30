namespace TaskMaster.DocumentService.SDK.DTOs;

/// <summary>
/// Request DTO for updating template metadata.
/// </summary>
public class UpdateTemplateRequest
{
    /// <summary>
    /// Gets or sets the template name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the template description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the list of available variables as JSON array.
    /// </summary>
    public string? AvailableVariables { get; set; }

    /// <summary>
    /// Gets or sets the template metadata as JSON string.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the category.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets whether the template is active.
    /// </summary>
    public bool? IsActive { get; set; }
}
