using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TaskMaster.DocumentService.Api.Authentication;
using Xunit;

namespace TaskMaster.DocumentService.Api.Tests.Authentication;

/// <summary>
/// Unit tests for <see cref="ApiKeyAuthenticationHandler"/>.
/// </summary>
public class ApiKeyAuthenticationHandlerTests
{
    private readonly Mock<IOptionsMonitor<ApiKeyAuthenticationOptions>> _authOptionsMonitor;
    private readonly Mock<ILoggerFactory> _loggerFactory;
    private readonly Mock<ILogger<ApiKeyAuthenticationHandler>> _logger;
    private readonly Mock<IOptions<ApiKeyOptions>> _apiKeyOptions;
    private readonly UrlEncoder _urlEncoder;
    private readonly DefaultHttpContext _httpContext;

    public ApiKeyAuthenticationHandlerTests()
    {
        _authOptionsMonitor = new Mock<IOptionsMonitor<ApiKeyAuthenticationOptions>>();
        _loggerFactory = new Mock<ILoggerFactory>();
        _logger = new Mock<ILogger<ApiKeyAuthenticationHandler>>();
        _apiKeyOptions = new Mock<IOptions<ApiKeyOptions>>();
        _urlEncoder = UrlEncoder.Default;
        _httpContext = new DefaultHttpContext();

        _authOptionsMonitor
            .Setup(x => x.Get(It.IsAny<string>()))
            .Returns(new ApiKeyAuthenticationOptions());

        _loggerFactory
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_logger.Object);
    }

    /// <summary>
    /// Test that authentication succeeds with a valid API key.
    /// </summary>
    [Fact]
    public async Task HandleAuthenticateAsync_WithValidApiKey_ReturnsSuccessResult()
    {
        // Arrange
        var validApiKey = "test-api-key-123";
        var apiKeyInfo = new ApiKeyInfo
        {
            TenantId = 1,
            TenantName = "Test Tenant",
            Description = "Test API Key",
            IsActive = true
        };

        var apiKeyOptions = new ApiKeyOptions
        {
            HeaderName = "X-API-Key",
            Keys = new Dictionary<string, ApiKeyInfo>
            {
                { validApiKey, apiKeyInfo }
            }
        };

        _apiKeyOptions.Setup(x => x.Value).Returns(apiKeyOptions);
        _httpContext.Request.Headers["X-API-Key"] = validApiKey;

        var handler = new ApiKeyAuthenticationHandler(
            _authOptionsMonitor.Object,
            _loggerFactory.Object,
            _urlEncoder,
            _apiKeyOptions.Object,
            _logger.Object);

        await handler.InitializeAsync(
            new AuthenticationScheme(ApiKeyAuthenticationOptions.SchemeName, null, typeof(ApiKeyAuthenticationHandler)),
            _httpContext);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Principal);
        Assert.Equal("1", result.Principal.FindFirst("TenantId")?.Value);
        Assert.Equal("Test Tenant", result.Principal.FindFirst("TenantName")?.Value);
        Assert.Equal("ApiKey", result.Principal.FindFirst("AuthenticationType")?.Value);
    }

    /// <summary>
    /// Test that authentication fails with an invalid API key.
    /// </summary>
    [Fact]
    public async Task HandleAuthenticateAsync_WithInvalidApiKey_ReturnsFailureResult()
    {
        // Arrange
        var apiKeyOptions = new ApiKeyOptions
        {
            HeaderName = "X-API-Key",
            Keys = new Dictionary<string, ApiKeyInfo>()
        };

        _apiKeyOptions.Setup(x => x.Value).Returns(apiKeyOptions);
        _httpContext.Request.Headers["X-API-Key"] = "invalid-key";

        var handler = new ApiKeyAuthenticationHandler(
            _authOptionsMonitor.Object,
            _loggerFactory.Object,
            _urlEncoder,
            _apiKeyOptions.Object,
            _logger.Object);

        await handler.InitializeAsync(
            new AuthenticationScheme(ApiKeyAuthenticationOptions.SchemeName, null, typeof(ApiKeyAuthenticationHandler)),
            _httpContext);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failure);
    }

    /// <summary>
    /// Test that authentication returns no result when API key header is missing.
    /// </summary>
    [Fact]
    public async Task HandleAuthenticateAsync_WithoutApiKeyHeader_ReturnsNoResult()
    {
        // Arrange
        var apiKeyOptions = new ApiKeyOptions
        {
            HeaderName = "X-API-Key",
            Keys = new Dictionary<string, ApiKeyInfo>()
        };

        _apiKeyOptions.Setup(x => x.Value).Returns(apiKeyOptions);
        // No header added

        var handler = new ApiKeyAuthenticationHandler(
            _authOptionsMonitor.Object,
            _loggerFactory.Object,
            _urlEncoder,
            _apiKeyOptions.Object,
            _logger.Object);

        await handler.InitializeAsync(
            new AuthenticationScheme(ApiKeyAuthenticationOptions.SchemeName, null, typeof(ApiKeyAuthenticationHandler)),
            _httpContext);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.None);
    }

    /// <summary>
    /// Test that authentication fails with an empty API key.
    /// </summary>
    [Fact]
    public async Task HandleAuthenticateAsync_WithEmptyApiKey_ReturnsFailureResult()
    {
        // Arrange
        var apiKeyOptions = new ApiKeyOptions
        {
            HeaderName = "X-API-Key",
            Keys = new Dictionary<string, ApiKeyInfo>()
        };

        _apiKeyOptions.Setup(x => x.Value).Returns(apiKeyOptions);
        _httpContext.Request.Headers["X-API-Key"] = "";

        var handler = new ApiKeyAuthenticationHandler(
            _authOptionsMonitor.Object,
            _loggerFactory.Object,
            _urlEncoder,
            _apiKeyOptions.Object,
            _logger.Object);

        await handler.InitializeAsync(
            new AuthenticationScheme(ApiKeyAuthenticationOptions.SchemeName, null, typeof(ApiKeyAuthenticationHandler)),
            _httpContext);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failure);
    }

    /// <summary>
    /// Test that authentication fails with an inactive API key.
    /// </summary>
    [Fact]
    public async Task HandleAuthenticateAsync_WithInactiveApiKey_ReturnsFailureResult()
    {
        // Arrange
        var validApiKey = "test-api-key-123";
        var apiKeyInfo = new ApiKeyInfo
        {
            TenantId = 1,
            TenantName = "Test Tenant",
            IsActive = false // Inactive key
        };

        var apiKeyOptions = new ApiKeyOptions
        {
            HeaderName = "X-API-Key",
            Keys = new Dictionary<string, ApiKeyInfo>
            {
                { validApiKey, apiKeyInfo }
            }
        };

        _apiKeyOptions.Setup(x => x.Value).Returns(apiKeyOptions);
        _httpContext.Request.Headers["X-API-Key"] = validApiKey;

        var handler = new ApiKeyAuthenticationHandler(
            _authOptionsMonitor.Object,
            _loggerFactory.Object,
            _urlEncoder,
            _apiKeyOptions.Object,
            _logger.Object);

        await handler.InitializeAsync(
            new AuthenticationScheme(ApiKeyAuthenticationOptions.SchemeName, null, typeof(ApiKeyAuthenticationHandler)),
            _httpContext);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failure);
        Assert.Contains("inactive", result.Failure.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test that all required claims are added to the principal.
    /// </summary>
    [Fact]
    public async Task HandleAuthenticateAsync_WithValidApiKey_AddsAllRequiredClaims()
    {
        // Arrange
        var validApiKey = "test-api-key-123";
        var apiKeyInfo = new ApiKeyInfo
        {
            TenantId = 42,
            TenantName = "Test Organization",
            Description = "Test Description",
            IsActive = true
        };

        var apiKeyOptions = new ApiKeyOptions
        {
            HeaderName = "X-API-Key",
            Keys = new Dictionary<string, ApiKeyInfo>
            {
                { validApiKey, apiKeyInfo }
            }
        };

        _apiKeyOptions.Setup(x => x.Value).Returns(apiKeyOptions);
        _httpContext.Request.Headers["X-API-Key"] = validApiKey;

        var handler = new ApiKeyAuthenticationHandler(
            _authOptionsMonitor.Object,
            _loggerFactory.Object,
            _urlEncoder,
            _apiKeyOptions.Object,
            _logger.Object);

        await handler.InitializeAsync(
            new AuthenticationScheme(ApiKeyAuthenticationOptions.SchemeName, null, typeof(ApiKeyAuthenticationHandler)),
            _httpContext);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        Assert.True(result.Succeeded);
        var principal = result.Principal;
        Assert.NotNull(principal);

        Assert.Equal("apikey-42", principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal("Test Organization", principal.FindFirst(ClaimTypes.Name)?.Value);
        Assert.Equal("42", principal.FindFirst("TenantId")?.Value);
        Assert.Equal("Test Organization", principal.FindFirst("TenantName")?.Value);
        Assert.Equal("ApiKey", principal.FindFirst("AuthenticationType")?.Value);
        Assert.Equal("Test Description", principal.FindFirst("Description")?.Value);
    }
}
