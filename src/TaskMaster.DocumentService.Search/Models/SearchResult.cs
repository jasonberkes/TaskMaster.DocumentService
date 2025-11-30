namespace TaskMaster.DocumentService.Search.Models;

/// <summary>
/// Represents the result of a search operation.
/// </summary>
/// <typeparam name="T">The type of the result items.</typeparam>
public class SearchResult<T>
{
    /// <summary>
    /// Gets or sets the search results.
    /// </summary>
    public IEnumerable<T> Hits { get; set; } = Enumerable.Empty<T>();

    /// <summary>
    /// Gets or sets the total number of results.
    /// </summary>
    public int TotalHits { get; set; }

    /// <summary>
    /// Gets or sets the number of results per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets the current page number (1-based).
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Gets or sets the total number of pages.
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Gets or sets the processing time in milliseconds.
    /// </summary>
    public int ProcessingTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the query that was executed.
    /// </summary>
    public string Query { get; set; } = string.Empty;
}
