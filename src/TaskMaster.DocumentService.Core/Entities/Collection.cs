namespace TaskMaster.DocumentService.Core.Entities;

/// <summary>
/// Represents a collection for organizing documents into publishable groups.
/// </summary>
public class Collection
{
    /// <summary>
    /// Gets or sets the unique identifier for the collection.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier this collection belongs to.
    /// </summary>
    public int TenantId { get; set; }

    /// <summary>
    /// Gets or sets the name of the collection.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the collection.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the slug for URL-friendly access.
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this collection is published.
    /// </summary>
    public bool IsPublished { get; set; }

    /// <summary>
    /// Gets or sets the publication timestamp.
    /// </summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// Gets or sets the user who published the collection.
    /// </summary>
    public string? PublishedBy { get; set; }

    /// <summary>
    /// Gets or sets the metadata as JSON.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the tags as JSON.
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Gets or sets the sort order for displaying collections.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user who created the collection.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user who last updated the collection.
    /// </summary>
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the collection is soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the deletion timestamp.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Gets or sets the user who deleted the collection.
    /// </summary>
    public string? DeletedBy { get; set; }

    /// <summary>
    /// Gets or sets the reason for deletion.
    /// </summary>
    public string? DeletedReason { get; set; }

    // Navigation properties
    /// <summary>
    /// Gets or sets the tenant this collection belongs to.
    /// </summary>
    public virtual Tenant Tenant { get; set; } = null!;

    /// <summary>
    /// Gets or sets the collection of documents in this collection.
    /// </summary>
    public virtual ICollection<CollectionDocument> CollectionDocuments { get; set; } = new List<CollectionDocument>();
}
