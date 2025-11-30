namespace TaskMaster.DocumentService.Core.DTOs;

/// <summary>
/// Search result containing document information and relevance scoring.
/// </summary>
public class DocumentSearchResult
{
    /// <summary>
    /// Gets or sets the document information.
    /// </summary>
    public DocumentDto Document { get; set; } = null!;

    /// <summary>
    /// Gets or sets the search relevance score.
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// Gets or sets the highlighted text snippets that match the query.
    /// </summary>
    public List<string>? Highlights { get; set; }
}
