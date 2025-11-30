namespace TaskMaster.DocumentService.Core.Entities;

/// <summary>
/// Represents a tenant in the multi-tenant document management system.
/// Supports hierarchical tenant relationships.
/// </summary>
public class Tenant
{
    /// <summary>
    /// Gets or sets the unique identifier for the tenant.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the parent tenant identifier for hierarchical relationships.
    /// </summary>
    public int? ParentTenantId { get; set; }

    /// <summary>
    /// Gets or sets the type of tenant (e.g., "Organization", "Department", "Team").
    /// </summary>
    public string TenantType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tenant name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique slug identifier for the tenant.
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tenant-specific settings as JSON.
    /// </summary>
    public string? Settings { get; set; }

    /// <summary>
    /// Gets or sets the retention policies as JSON.
    /// </summary>
    public string? RetentionPolicies { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the tenant is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the parent tenant navigation property.
    /// </summary>
    public virtual Tenant? ParentTenant { get; set; }

    /// <summary>
    /// Gets or sets the collection of child tenants.
    /// </summary>
    public virtual ICollection<Tenant> ChildTenants { get; set; } = new List<Tenant>();

    /// <summary>
    /// Gets or sets the collection of documents belonging to this tenant.
    /// </summary>
    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
}
