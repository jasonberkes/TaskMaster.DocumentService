namespace TaskMaster.DocumentService.Core.DTOs;

/// <summary>
/// Response containing paginated search results.
/// </summary>
public class DocumentSearchResponse
{
    /// <summary>
    /// Gets or sets the search results.
    /// </summary>
    public List<DocumentSearchResult> Results { get; set; } = new();

    /// <summary>
    /// Gets or sets the total number of matching documents.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the current page number.
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets the total number of pages.
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Gets or sets the processing time in milliseconds.
    /// </summary>
    public long ProcessingTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the search query that was executed.
    /// </summary>
    public string Query { get; set; } = string.Empty;
}
