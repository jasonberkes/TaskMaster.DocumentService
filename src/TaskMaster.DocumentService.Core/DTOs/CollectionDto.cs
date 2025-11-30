namespace TaskMaster.DocumentService.Core.DTOs;

/// <summary>
/// Data transfer object for collection information.
/// </summary>
public class CollectionDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the collection.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the tenant ID that owns this collection.
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
    /// Gets or sets the URL-friendly slug for the collection.
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the status of the collection.
    /// </summary>
    public string Status { get; set; } = "Draft";

    /// <summary>
    /// Gets or sets a value indicating whether the collection is published.
    /// </summary>
    public bool IsPublished { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the collection was published.
    /// </summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// Gets or sets the user who published the collection.
    /// </summary>
    public string? PublishedBy { get; set; }

    /// <summary>
    /// Gets or sets the URL of the cover image for the collection.
    /// </summary>
    public string? CoverImageUrl { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets tags associated with the collection.
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Gets or sets the sort order for displaying the collection.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the collection was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user who created the collection.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the collection was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user who last updated the collection.
    /// </summary>
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Gets or sets the number of documents in the collection.
    /// </summary>
    public int DocumentCount { get; set; }
}
