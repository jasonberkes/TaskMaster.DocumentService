namespace TaskMaster.DocumentService.Core.Entities;

/// <summary>
/// Represents a tenant in the document service with hierarchical support
/// </summary>
public class Tenant
{
    /// <summary>
    /// Gets or sets the unique identifier for the tenant
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the parent tenant identifier for hierarchical tenants
    /// </summary>
    public int? ParentTenantId { get; set; }

    /// <summary>
    /// Gets or sets the type of tenant (e.g., Organization, Department, Project)
    /// </summary>
    public string TenantType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the tenant
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL-friendly slug for the tenant
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets tenant-specific settings as JSON
    /// </summary>
    public string? Settings { get; set; }

    /// <summary>
    /// Gets or sets retention policies as JSON
    /// </summary>
    public string? RetentionPolicies { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last update timestamp
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets whether the tenant is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Navigation property for parent tenant
    /// </summary>
    public Tenant? ParentTenant { get; set; }

    /// <summary>
    /// Navigation property for child tenants
    /// </summary>
    public ICollection<Tenant> ChildTenants { get; set; } = new List<Tenant>();

    /// <summary>
    /// Navigation property for documents
    /// </summary>
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}
