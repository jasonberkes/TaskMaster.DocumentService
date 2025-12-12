namespace TaskMaster.DocumentService.SDK.DTOs;

/// <summary>
/// Search request parameters.
/// </summary>
public class SearchRequestDto
{
    /// <summary>
    /// The search query text.
    /// </summary>
    public string? Query { get; set; }

    /// <summary>
    /// Filter by document type ID.
    /// </summary>
    public int? DocumentTypeId { get; set; }

    /// <summary>
    /// Filter by tags (comma-separated).
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Filter by MIME type.
    /// </summary>
    public string? MimeType { get; set; }

    /// <summary>
    /// Only return current versions (default: true).
    /// </summary>
    public bool OnlyCurrentVersion { get; set; } = true;

    /// <summary>
    /// Filter by created date from.
    /// </summary>
    public DateTime? CreatedFrom { get; set; }

    /// <summary>
    /// Filter by created date to.
    /// </summary>
    public DateTime? CreatedTo { get; set; }

    /// <summary>
    /// Page number (1-based, default: 1).
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Page size (default: 20, max: 100).
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Sort by field (e.g., "createdAt", "title", "updatedAt").
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Sort order ("asc" or "desc", default: "desc").
    /// </summary>
    public string? SortOrder { get; set; } = "desc";
}

/// <summary>
/// Search result response.
/// </summary>
public class SearchResultDto
{
    /// <summary>
    /// The search results.
    /// </summary>
    public List<SearchHitDto> Hits { get; set; } = new();

    /// <summary>
    /// Total number of matching documents.
    /// </summary>
    public int TotalHits { get; set; }

    /// <summary>
    /// Current page number.
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Results per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages.
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Processing time in milliseconds.
    /// </summary>
    public int ProcessingTimeMs { get; set; }

    /// <summary>
    /// The query that was executed.
    /// </summary>
    public string Query { get; set; } = string.Empty;
}

/// <summary>
/// Individual search hit.
/// </summary>
public class SearchHitDto
{
    /// <summary>
    /// Document ID.
    /// </summary>
    public long DocumentId { get; set; }

    /// <summary>
    /// Tenant ID.
    /// </summary>
    public int TenantId { get; set; }

    /// <summary>
    /// Document type ID.
    /// </summary>
    public int DocumentTypeId { get; set; }

    /// <summary>
    /// Document title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Document description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Original file name.
    /// </summary>
    public string? OriginalFileName { get; set; }

    /// <summary>
    /// MIME type.
    /// </summary>
    public string? MimeType { get; set; }

    /// <summary>
    /// Tags as JSON string.
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Version number.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Whether this is the current version.
    /// </summary>
    public bool IsCurrentVersion { get; set; }

    /// <summary>
    /// Created timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Created by user.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Updated timestamp.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
