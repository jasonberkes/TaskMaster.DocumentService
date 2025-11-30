namespace TaskMaster.DocumentService.Core.DTOs;

/// <summary>
/// Data transfer object for creating a new tenant.
/// </summary>
public class CreateTenantDto
{
    /// <summary>
    /// Gets or sets the parent tenant identifier for hierarchical structure.
    /// Null indicates a root-level tenant.
    /// </summary>
    public int? ParentTenantId { get; set; }

    /// <summary>
    /// Gets or sets the type of tenant (e.g., "Organization", "Department", "Team").
    /// </summary>
    public string TenantType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the tenant.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique slug identifier for the tenant (URL-friendly).
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tenant-specific settings in JSON format.
    /// </summary>
    public string? Settings { get; set; }

    /// <summary>
    /// Gets or sets the document retention policies in JSON format.
    /// </summary>
    public string? RetentionPolicies { get; set; }
}
