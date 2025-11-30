namespace TaskMaster.DocumentService.Api.Authentication;

/// <summary>
/// Configuration options for JWT authentication.
/// </summary>
public class JwtOptions
{
    /// <summary>
    /// Gets the configuration section name.
    /// </summary>
    public const string SectionName = "Jwt";

    /// <summary>
    /// Gets or sets the JWT secret key used for token validation.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the valid issuer for JWT tokens.
    /// </summary>
    public string ValidIssuer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the valid audience for JWT tokens.
    /// </summary>
    public string ValidAudience { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether HTTPS metadata is required.
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the token lifetime should be validated.
    /// </summary>
    public bool ValidateLifetime { get; set; } = true;

    /// <summary>
    /// Gets or sets the clock skew for token validation in minutes.
    /// </summary>
    public int ClockSkewMinutes { get; set; } = 5;
}
