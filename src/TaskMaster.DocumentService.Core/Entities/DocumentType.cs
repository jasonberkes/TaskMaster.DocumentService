namespace TaskMaster.DocumentService.Core.Entities;

/// <summary>
/// Represents a document type for classification and metadata schema definition.
/// </summary>
public class DocumentType
{
    /// <summary>
    /// Gets or sets the unique identifier for the document type.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the unique name of the document type.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the document type.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the document type.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the metadata schema as JSON.
    /// </summary>
    public string? MetadataSchema { get; set; }

    /// <summary>
    /// Gets or sets the default tags as JSON array.
    /// </summary>
    public string? DefaultTags { get; set; }

    /// <summary>
    /// Gets or sets the icon identifier for the document type.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether content should be indexed for search.
    /// </summary>
    public bool IsContentIndexed { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether this type has an extension table.
    /// </summary>
    public bool HasExtensionTable { get; set; }

    /// <summary>
    /// Gets or sets the name of the extension table if applicable.
    /// </summary>
    public string? ExtensionTableName { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the document type was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the document type is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the collection of documents of this type.
    /// </summary>
    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
}
