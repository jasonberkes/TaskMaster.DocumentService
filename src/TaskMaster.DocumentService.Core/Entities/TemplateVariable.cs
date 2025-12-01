namespace TaskMaster.DocumentService.Core.Entities;

/// <summary>
/// Represents a variable definition for a document template.
/// </summary>
public class TemplateVariable
{
    /// <summary>
    /// Gets or sets the unique identifier for the variable.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the template identifier this variable belongs to.
    /// </summary>
    public int TemplateId { get; set; }

    /// <summary>
    /// Gets or sets the variable name (used in template as {{variableName}}).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name for the variable.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the variable.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the data type of the variable (e.g., string, number, date, boolean).
    /// </summary>
    public string DataType { get; set; } = "string";

    /// <summary>
    /// Gets or sets the default value for the variable.
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this variable is required.
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Gets or sets the validation regex pattern for the variable value.
    /// </summary>
    public string? ValidationPattern { get; set; }

    /// <summary>
    /// Gets or sets the validation error message.
    /// </summary>
    public string? ValidationMessage { get; set; }

    /// <summary>
    /// Gets or sets the sort order for displaying variables.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    /// <summary>
    /// Gets or sets the template this variable belongs to.
    /// </summary>
    public virtual DocumentTemplate Template { get; set; } = null!;
}
