namespace TaskMaster.DocumentService.Core.Models;

/// <summary>
/// Represents the result of rendering a template with variable substitution.
/// </summary>
public class TemplateRenderResult
{
    /// <summary>
    /// Gets or sets the rendered content with all variables substituted.
    /// </summary>
    public string RenderedContent { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the rendering was successful.
    /// </summary>
    public bool IsSuccess { get; set; } = true;

    /// <summary>
    /// Gets or sets the list of warnings encountered during rendering (e.g., missing optional variables).
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of errors encountered during rendering (e.g., missing required variables).
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Gets or sets the dictionary of variables that were successfully substituted.
    /// </summary>
    public Dictionary<string, string> SubstitutedVariables { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of missing variables that were not provided.
    /// </summary>
    public List<string> MissingVariables { get; set; } = new();
}
