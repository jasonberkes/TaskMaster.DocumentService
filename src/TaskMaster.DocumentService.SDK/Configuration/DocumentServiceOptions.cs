namespace TaskMaster.DocumentService.SDK.Configuration;

/// <summary>
/// Configuration options for the Document Service SDK.
/// </summary>
public class DocumentServiceOptions
{
    /// <summary>
    /// Gets or sets the base URL of the Document Service API.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the API key for authentication (if required).
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the timeout for HTTP requests in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 100;

    /// <summary>
    /// Gets or sets a value indicating whether to retry failed requests.
    /// </summary>
    public bool EnableRetry { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;
}
