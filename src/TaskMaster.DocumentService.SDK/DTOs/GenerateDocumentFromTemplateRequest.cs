using System.Collections.Generic;

namespace TaskMaster.DocumentService.SDK.DTOs;

/// <summary>
/// Request DTO for generating a document from a template with variable substitution.
/// </summary>
public class GenerateDocumentFromTemplateRequest
{
    /// <summary>
    /// Gets or sets the template identifier to use.
    /// </summary>
    public long TemplateId { get; set; }

    /// <summary>
    /// Gets or sets the title for the generated document.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description for the generated document.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the variable substitutions as a dictionary.
    /// Key: variable name (e.g., "customerName")
    /// Value: substitution value (e.g., "John Doe")
    /// </summary>
    public Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets optional metadata for the generated document as JSON string.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets optional tags for the generated document as JSON string.
    /// </summary>
    public string? Tags { get; set; }
}
