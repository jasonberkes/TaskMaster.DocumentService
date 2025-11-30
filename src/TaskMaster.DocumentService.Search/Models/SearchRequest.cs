namespace TaskMaster.DocumentService.Search.Models;

/// <summary>
/// Represents a search request with filtering and pagination options.
/// </summary>
public class SearchRequest
{
    /// <summary>
    /// Gets or sets the search query text.
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tenant identifier to filter by.
    /// </summary>
    public int? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the document type identifier to filter by.
    /// </summary>
    public int? DocumentTypeId { get; set; }

    /// <summary>
    /// Gets or sets the tags to filter by (comma-separated).
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Gets or sets the MIME type to filter by.
    /// </summary>
    public string? MimeType { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to include only current versions.
    /// </summary>
    public bool OnlyCurrentVersion { get; set; } = true;

    /// <summary>
    /// Gets or sets the created date from filter.
    /// </summary>
    public DateTime? CreatedFrom { get; set; }

    /// <summary>
    /// Gets or sets the created date to filter.
    /// </summary>
    public DateTime? CreatedTo { get; set; }

    /// <summary>
    /// Gets or sets the page number (1-based).
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Gets or sets the fields to search in. If empty, searches all fields.
    /// </summary>
    public string[]? SearchableAttributes { get; set; }

    /// <summary>
    /// Gets or sets the sort criteria (e.g., "createdAt:desc").
    /// </summary>
    public string[]? Sort { get; set; }
}
