namespace TaskMaster.DocumentService.Core.DTOs;

/// <summary>
/// Data transfer object for updating an existing tenant.
/// </summary>
public class UpdateTenantDto
{
    /// <summary>
    /// Gets or sets the display name of the tenant.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the tenant-specific settings in JSON format.
    /// </summary>
    public string? Settings { get; set; }

    /// <summary>
    /// Gets or sets the document retention policies in JSON format.
    /// </summary>
    public string? RetentionPolicies { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the tenant is active.
    /// </summary>
    public bool? IsActive { get; set; }
}
