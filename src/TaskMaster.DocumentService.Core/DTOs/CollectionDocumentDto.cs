namespace TaskMaster.DocumentService.Core.DTOs;

/// <summary>
/// Data transfer object for a document within a collection.
/// </summary>
public class CollectionDocumentDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the collection-document relationship.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the collection ID.
    /// </summary>
    public long CollectionId { get; set; }

    /// <summary>
    /// Gets or sets the document ID.
    /// </summary>
    public long DocumentId { get; set; }

    /// <summary>
    /// Gets or sets the sort order for the document within the collection.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the document was added to the collection.
    /// </summary>
    public DateTime AddedAt { get; set; }

    /// <summary>
    /// Gets or sets the user who added the document to the collection.
    /// </summary>
    public string? AddedBy { get; set; }

    /// <summary>
    /// Gets or sets optional notes about the document in this collection.
    /// </summary>
    public string? Notes { get; set; }
}
