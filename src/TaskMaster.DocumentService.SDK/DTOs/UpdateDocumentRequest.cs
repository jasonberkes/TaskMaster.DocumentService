namespace TaskMaster.DocumentService.SDK.DTOs;

/// <summary>
/// Request object for updating an existing document.
/// </summary>
public class UpdateDocumentRequest
{
    /// <summary>
    /// Gets or sets the title of the document.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the description of the document.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the metadata as JSON string.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the tags as JSON string.
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Gets or sets the user updating the document.
    /// </summary>
    public string? UpdatedBy { get; set; }
}
