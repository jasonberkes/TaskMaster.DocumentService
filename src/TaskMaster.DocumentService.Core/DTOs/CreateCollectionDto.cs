using System.ComponentModel.DataAnnotations;

namespace TaskMaster.DocumentService.Core.DTOs;

/// <summary>
/// Data transfer object for creating a new collection.
/// </summary>
public class CreateCollectionDto
{
    /// <summary>
    /// Gets or sets the tenant ID that owns this collection.
    /// </summary>
    [Required]
    public int TenantId { get; set; }

    /// <summary>
    /// Gets or sets the name of the collection.
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the collection.
    /// </summary>
    [StringLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the URL-friendly slug for the collection.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    [RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = "Slug must be lowercase alphanumeric with hyphens only")]
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL of the cover image for the collection.
    /// </summary>
    [StringLength(500)]
    [Url]
    public string? CoverImageUrl { get; set; }

    /// <summary>
    /// Gets or sets additional metadata in JSON format.
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
}
