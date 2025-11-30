namespace TaskMaster.DocumentService.Core.Entities;

/// <summary>
/// Represents the many-to-many relationship between collections and documents.
/// </summary>
public class CollectionDocument
{
    /// <summary>
    /// Gets or sets the collection identifier.
    /// </summary>
    public long CollectionId { get; set; }

    /// <summary>
    /// Gets or sets the document identifier.
    /// </summary>
    public long DocumentId { get; set; }

    /// <summary>
    /// Gets or sets the sort order of the document within the collection.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the document was added to the collection.
    /// </summary>
    public DateTime AddedAt { get; set; }

    /// <summary>
    /// Gets or sets the user who added the document to the collection.
    /// </summary>
    public string? AddedBy { get; set; }

    /// <summary>
    /// Gets or sets optional metadata for this collection-document relationship as JSON.
    /// </summary>
    public string? Metadata { get; set; }

    // Navigation properties
    /// <summary>
    /// Gets or sets the collection.
    /// </summary>
    public virtual Collection Collection { get; set; } = null!;

    /// <summary>
    /// Gets or sets the document.
    /// </summary>
    public virtual Document Document { get; set; } = null!;
}
