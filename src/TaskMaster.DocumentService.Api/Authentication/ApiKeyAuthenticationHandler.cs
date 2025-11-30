using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace TaskMaster.DocumentService.Api.Authentication;

/// <summary>
/// Authentication handler for API key-based authentication.
/// </summary>
public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly ApiKeyOptions _apiKeyOptions;
    private readonly ILogger<ApiKeyAuthenticationHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiKeyAuthenticationHandler"/> class.
    /// </summary>
    /// <param name="options">The authentication options.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="encoder">The URL encoder.</param>
    /// <param name="apiKeyOptions">The API key configuration options.</param>
    /// <param name="logger">The logger.</param>
    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory loggerFactory,
        UrlEncoder encoder,
        IOptions<ApiKeyOptions> apiKeyOptions,
        ILogger<ApiKeyAuthenticationHandler> logger)
        : base(options, loggerFactory, encoder)
    {
        _apiKeyOptions = apiKeyOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Handles the authentication request.
    /// </summary>
    /// <returns>The authentication result.</returns>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check if the API key header exists
        if (!Request.Headers.TryGetValue(_apiKeyOptions.HeaderName, out var apiKeyHeaderValues))
        {
            _logger.LogDebug("API key header '{HeaderName}' not found", _apiKeyOptions.HeaderName);
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var providedApiKey = apiKeyHeaderValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(providedApiKey))
        {
            _logger.LogWarning("API key header '{HeaderName}' is empty", _apiKeyOptions.HeaderName);
            return Task.FromResult(AuthenticateResult.Fail("API key is empty"));
        }

        // Validate the API key
        if (!_apiKeyOptions.Keys.TryGetValue(providedApiKey, out var apiKeyInfo))
        {
            _logger.LogWarning("Invalid API key provided");
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));
        }

        // Check if the API key is active
        if (!apiKeyInfo.IsActive)
        {
            _logger.LogWarning("Inactive API key used for tenant {TenantId}", apiKeyInfo.TenantId);
            return Task.FromResult(AuthenticateResult.Fail("API key is inactive"));
        }

        // Create claims for the authenticated user
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, $"apikey-{apiKeyInfo.TenantId}"),
            new(ClaimTypes.Name, apiKeyInfo.TenantName),
            new("TenantId", apiKeyInfo.TenantId.ToString()),
            new("TenantName", apiKeyInfo.TenantName),
            new("AuthenticationType", "ApiKey")
        };

        if (!string.IsNullOrEmpty(apiKeyInfo.Description))
        {
            claims.Add(new Claim("Description", apiKeyInfo.Description));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        _logger.LogInformation("API key authentication successful for tenant {TenantId}", apiKeyInfo.TenantId);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    /// <summary>
    /// Handles the authentication challenge.
    /// </summary>
    /// <param name="properties">The authentication properties.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 401;
        Response.Headers.Append("WWW-Authenticate", $"ApiKey realm=\"{_apiKeyOptions.HeaderName}\"");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the forbidden response.
    /// </summary>
    /// <param name="properties">The authentication properties.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 403;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Authentication options for API key authentication.
/// </summary>
public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    /// <summary>
    /// Gets the authentication scheme name.
    /// </summary>
    public const string SchemeName = "ApiKey";
}
