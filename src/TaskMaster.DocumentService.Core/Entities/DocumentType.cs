namespace TaskMaster.DocumentService.Core.Entities;

/// <summary>
/// Represents a document type with associated metadata schema
/// </summary>
public class DocumentType
{
    /// <summary>
    /// Gets or sets the unique identifier for the document type
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the internal name of the document type
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name shown to users
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the document type
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the JSON schema for document metadata validation
    /// </summary>
    public string? MetadataSchema { get; set; }

    /// <summary>
    /// Gets or sets default tags applied to documents of this type
    /// </summary>
    public string? DefaultTags { get; set; }

    /// <summary>
    /// Gets or sets the icon name or path for UI display
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Gets or sets whether document content should be indexed for search
    /// </summary>
    public bool IsContentIndexed { get; set; }

    /// <summary>
    /// Gets or sets whether this type uses an extension table for additional fields
    /// </summary>
    public bool HasExtensionTable { get; set; }

    /// <summary>
    /// Gets or sets the name of the extension table if HasExtensionTable is true
    /// </summary>
    public string? ExtensionTableName { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets whether the document type is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Navigation property for documents of this type
    /// </summary>
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}
