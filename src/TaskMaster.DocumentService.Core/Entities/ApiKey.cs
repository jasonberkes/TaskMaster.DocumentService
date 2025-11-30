namespace TaskMaster.DocumentService.Core.Entities;

/// <summary>
/// Represents an API key for authenticating service requests.
/// </summary>
public class ApiKey
{
    /// <summary>
    /// Gets or sets the unique identifier for the API key.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier that owns this API key.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the API key value (hashed).
    /// </summary>
    public string KeyHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the API key name/description.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the API key is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the date and time when the API key was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the API key expires.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the API key was last used.
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Gets or sets the tenant that owns this API key.
    /// </summary>
    public Tenant Tenant { get; set; } = null!;
}
