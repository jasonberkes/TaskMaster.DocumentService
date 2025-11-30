namespace TaskMaster.DocumentService.SDK.DTOs;

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
    /// Gets or sets the document type identifier.
    /// </summary>
    public int DocumentTypeId { get; set; }

    /// <summary>
    /// Gets or sets the template name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the list of available variables as JSON array.
    /// Example: ["customerName", "invoiceDate", "totalAmount"]
    /// </summary>
    public string? AvailableVariables { get; set; }

    /// <summary>
    /// Gets or sets the template metadata as JSON string.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the category for organizing templates.
    /// </summary>
    public string? Category { get; set; }
}
