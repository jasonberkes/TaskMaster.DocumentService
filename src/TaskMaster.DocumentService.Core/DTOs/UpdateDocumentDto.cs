namespace TaskMaster.DocumentService.Core.DTOs;

/// <summary>
/// DTO for updating an existing document
/// </summary>
public class UpdateDocumentDto
{
    /// <summary>
    /// Gets or sets the document title
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the document description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets custom metadata as JSON string
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets tags as JSON array string
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Gets or sets the user updating the document
    /// </summary>
    public string? UpdatedBy { get; set; }
}
