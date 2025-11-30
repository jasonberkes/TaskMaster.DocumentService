namespace TaskMaster.DocumentService.Core.Entities;

/// <summary>
/// Represents a configurable document type with metadata schema and indexing settings.
/// </summary>
public class DocumentType
{
    /// <summary>
    /// Gets or sets the unique identifier for the document type.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the internal name (identifier) for the document type.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name for the document type.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the document type.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the metadata schema as JSON Schema.
    /// </summary>
    public string? MetadataSchema { get; set; }

    /// <summary>
    /// Gets or sets the default tags as JSON array.
    /// </summary>
    public string? DefaultTags { get; set; }

    /// <summary>
    /// Gets or sets the icon identifier for UI display.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether content should be indexed for search.
    /// </summary>
    public bool IsContentIndexed { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether this document type has an extension table.
    /// </summary>
    public bool HasExtensionTable { get; set; } = false;

    /// <summary>
    /// Gets or sets the name of the extension table if HasExtensionTable is true.
    /// </summary>
    public string? ExtensionTableName { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets a value indicating whether the document type is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the collection of documents of this type.
    /// </summary>
    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
}
