namespace TaskMaster.DocumentService.Core.Entities;

/// <summary>
/// Represents a log entry tracking template usage for analytics and auditing.
/// </summary>
public class TemplateUsageLog
{
    /// <summary>
    /// Gets or sets the unique identifier for the usage log entry.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the template identifier that was used.
    /// </summary>
    public int TemplateId { get; set; }

    /// <summary>
    /// Gets or sets the template navigation property.
    /// </summary>
    public DocumentTemplate? Template { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    public int TenantId { get; set; }

    /// <summary>
    /// Gets or sets the document identifier that was created from the template (if applicable).
    /// </summary>
    public long? DocumentId { get; set; }

    /// <summary>
    /// Gets or sets the JSON object containing the variable values used during substitution.
    /// </summary>
    public string? VariablesUsed { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the template was used.
    /// </summary>
    public DateTime UsedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the user who used the template.
    /// </summary>
    public string? UsedBy { get; set; }

    /// <summary>
    /// Gets or sets the status of the template usage (Success, Failed, PartialSuccess).
    /// </summary>
    public string Status { get; set; } = "Success";

    /// <summary>
    /// Gets or sets the error message if the template usage failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
