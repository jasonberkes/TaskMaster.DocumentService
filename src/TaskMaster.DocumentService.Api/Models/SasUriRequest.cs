namespace TaskMaster.DocumentService.Api.Models;

/// <summary>
/// Request model for generating a SAS URI.
/// </summary>
public class SasUriRequest
{
    /// <summary>
    /// Gets or sets the container name.
    /// </summary>
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the blob name.
    /// </summary>
    public string BlobName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expiration duration in hours.
    /// Default is 1 hour.
    /// </summary>
    public int ExpiresInHours { get; set; } = 1;
}
