namespace TaskMaster.DocumentService.Core.DTOs;

/// <summary>
/// Response DTO for rendered template content.
/// </summary>
public class RenderTemplateResponse
{
    /// <summary>
    /// Gets or sets the rendered content with all variables substituted.
    /// </summary>
    public string RenderedContent { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the rendering was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the list of warnings.
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of errors.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Gets or sets the dictionary of substituted variables.
    /// </summary>
    public Dictionary<string, string> SubstitutedVariables { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of missing variables.
    /// </summary>
    public List<string> MissingVariables { get; set; } = new();
}
