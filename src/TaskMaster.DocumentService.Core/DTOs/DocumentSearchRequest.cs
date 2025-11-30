namespace TaskMaster.DocumentService.Core.DTOs;

/// <summary>
/// Request parameters for document search.
/// </summary>
public class DocumentSearchRequest
{
    /// <summary>
    /// Gets or sets the search query string.
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tenant identifier to filter results.
    /// </summary>
    public int? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the document type identifier to filter results.
    /// </summary>
    public int? DocumentTypeId { get; set; }

    /// <summary>
    /// Gets or sets the tags to filter results.
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Gets or sets the page number for pagination (1-based).
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size for pagination.
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Gets or sets a value indicating whether to include archived documents.
    /// </summary>
    public bool IncludeArchived { get; set; }

    /// <summary>
    /// Gets or sets the sort field.
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to sort in descending order.
    /// </summary>
    public bool SortDescending { get; set; }
}
