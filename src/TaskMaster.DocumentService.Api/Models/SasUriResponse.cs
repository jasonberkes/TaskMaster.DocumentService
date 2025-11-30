namespace TaskMaster.DocumentService.Api.Models;

/// <summary>
/// Response model for SAS URI generation.
/// </summary>
public class SasUriResponse
{
    /// <summary>
    /// Gets or sets the SAS URI.
    /// </summary>
    public string SasUri { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expiration time.
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}
