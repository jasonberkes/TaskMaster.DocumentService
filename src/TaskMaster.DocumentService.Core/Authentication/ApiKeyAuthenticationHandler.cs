using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TaskMaster.DocumentService.Core.Authentication;

/// <summary>
/// Authentication handler for API key-based authentication.
/// </summary>
public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private const string ApiKeyHeaderName = "X-API-Key";
    private readonly IApiKeyValidator _apiKeyValidator;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiKeyAuthenticationHandler"/> class.
    /// </summary>
    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IApiKeyValidator apiKeyValidator)
        : base(options, logger, encoder)
    {
        _apiKeyValidator = apiKeyValidator;
    }

    /// <summary>
    /// Handles the authentication request.
    /// </summary>
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyHeaderValues))
        {
            return AuthenticateResult.NoResult();
        }

        var providedApiKey = apiKeyHeaderValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(providedApiKey))
        {
            return AuthenticateResult.NoResult();
        }

        try
        {
            var validationResult = await _apiKeyValidator.ValidateAsync(providedApiKey);

            if (!validationResult.IsValid)
            {
                Logger.LogWarning("Invalid API key attempt");
                return AuthenticateResult.Fail("Invalid API key");
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, validationResult.ApiKeyName!),
                new Claim("TenantId", validationResult.TenantId.ToString()),
                new Claim("AuthType", "ApiKey")
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            Logger.LogInformation("API key authentication successful for tenant {TenantId}", validationResult.TenantId);

            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during API key authentication");
            return AuthenticateResult.Fail("Error processing API key");
        }
    }
}

/// <summary>
/// Options for API key authentication.
/// </summary>
public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "ApiKey";
}

/// <summary>
/// Interface for validating API keys.
/// </summary>
public interface IApiKeyValidator
{
    /// <summary>
    /// Validates an API key.
    /// </summary>
    /// <param name="apiKey">The API key to validate.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the validation result.</returns>
    Task<ApiKeyValidationResult> ValidateAsync(string apiKey);
}

/// <summary>
/// Result of API key validation.
/// </summary>
public class ApiKeyValidationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the API key is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the tenant ID associated with the API key.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the API key name.
    /// </summary>
    public string? ApiKeyName { get; set; }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ApiKeyValidationResult Success(Guid tenantId, string apiKeyName)
        => new() { IsValid = true, TenantId = tenantId, ApiKeyName = apiKeyName };

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    public static ApiKeyValidationResult Failure()
        => new() { IsValid = false };
}

/// <summary>
/// Utility class for hashing API keys.
/// </summary>
public static class ApiKeyHasher
{
    /// <summary>
    /// Hashes an API key using SHA256.
    /// </summary>
    /// <param name="apiKey">The API key to hash.</param>
    /// <returns>The hashed API key.</returns>
    public static string HashApiKey(string apiKey)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(apiKey);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
