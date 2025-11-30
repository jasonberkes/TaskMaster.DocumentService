namespace TaskMaster.DocumentService.Core.Models;

/// <summary>
/// Represents a variable definition in a document template.
/// </summary>
public class TemplateVariable
{
    /// <summary>
    /// Gets or sets the variable name (used in placeholders like {{variableName}}).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the variable display label.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the variable type (Text, Number, Date, Boolean, Selection).
    /// </summary>
    public string Type { get; set; } = "Text";

    /// <summary>
    /// Gets or sets the default value for the variable.
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the variable is required.
    /// </summary>
    public bool IsRequired { get; set; } = false;

    /// <summary>
    /// Gets or sets the validation pattern (regex) for the variable value.
    /// </summary>
    public string? ValidationPattern { get; set; }

    /// <summary>
    /// Gets or sets the help text or description for the variable.
    /// </summary>
    public string? HelpText { get; set; }

    /// <summary>
    /// Gets or sets the available options for Selection type variables.
    /// </summary>
    public List<string>? Options { get; set; }
}
