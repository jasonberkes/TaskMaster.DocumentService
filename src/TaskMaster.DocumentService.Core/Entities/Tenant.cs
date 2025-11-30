namespace TaskMaster.DocumentService.Core.Entities;

/// <summary>
/// Represents a tenant in the multi-tenant document service.
/// Supports hierarchical tenant structure with parent-child relationships.
/// </summary>
public class Tenant
{
    /// <summary>
    /// Gets or sets the unique identifier for the tenant.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the parent tenant identifier for hierarchical tenant structure.
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
    /// Gets or sets the document retention policies for the tenant in JSON format.
    /// </summary>
    public string? RetentionPolicies { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the tenant was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the tenant was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the tenant is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Navigation property to the parent tenant.
    /// </summary>
    public virtual Tenant? ParentTenant { get; set; }

    /// <summary>
    /// Navigation property to child tenants.
    /// </summary>
    public virtual ICollection<Tenant> ChildTenants { get; set; } = new List<Tenant>();
}
