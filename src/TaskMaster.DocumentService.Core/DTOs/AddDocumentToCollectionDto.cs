using System.ComponentModel.DataAnnotations;

namespace TaskMaster.DocumentService.Core.DTOs;

/// <summary>
/// Data transfer object for adding a document to a collection.
/// </summary>
public class AddDocumentToCollectionDto
{
    /// <summary>
    /// Gets or sets the document ID to add to the collection.
    /// </summary>
    [Required]
    public long DocumentId { get; set; }

    /// <summary>
    /// Gets or sets the sort order for the document within the collection.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Gets or sets optional notes about the document in this collection.
    /// </summary>
    [StringLength(1000)]
    public string? Notes { get; set; }
}
