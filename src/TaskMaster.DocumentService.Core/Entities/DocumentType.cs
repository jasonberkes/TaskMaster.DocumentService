namespace TaskMaster.DocumentService.Core.Entities;

/// <summary>
/// Represents a document type with metadata schema definition.
/// </summary>
public class DocumentType
{
    /// <summary>
    /// Gets or sets the unique identifier for the document type.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the internal name of the document type.
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
    /// Gets or sets the default tags as JSON.
    /// </summary>
    public string? DefaultTags { get; set; }

    /// <summary>
    /// Gets or sets the icon identifier for the document type.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether content should be indexed.
    /// </summary>
    public bool IsContentIndexed { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this type has an extension table.
    /// </summary>
    public bool HasExtensionTable { get; set; }

    /// <summary>
    /// Gets or sets the extension table name if applicable.
    /// </summary>
    public string? ExtensionTableName { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the document type is active.
    /// </summary>
    public bool IsActive { get; set; }

    // Navigation properties
    /// <summary>
    /// Gets or sets the documents of this type.
    /// </summary>
    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
}
