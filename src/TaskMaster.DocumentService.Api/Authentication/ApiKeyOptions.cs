namespace TaskMaster.DocumentService.Api.Authentication;

/// <summary>
/// Configuration options for API key authentication.
/// </summary>
public class ApiKeyOptions
{
    /// <summary>
    /// Gets the configuration section name.
    /// </summary>
    public const string SectionName = "ApiKey";

    /// <summary>
    /// Gets or sets the header name for API key authentication.
    /// </summary>
    public string HeaderName { get; set; } = "X-API-Key";

    /// <summary>
    /// Gets or sets the valid API keys with their associated tenant IDs and names.
    /// Key: API key value, Value: Tuple of (TenantId, TenantName)
    /// </summary>
    public Dictionary<string, ApiKeyInfo> Keys { get; set; } = new();
}

/// <summary>
/// Information associated with an API key.
/// </summary>
public class ApiKeyInfo
{
    /// <summary>
    /// Gets or sets the tenant ID associated with this API key.
    /// </summary>
    public int TenantId { get; set; }

    /// <summary>
    /// Gets or sets the tenant name associated with this API key.
    /// </summary>
    public string TenantName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of this API key.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this API key is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
