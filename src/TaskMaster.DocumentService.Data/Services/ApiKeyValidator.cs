using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskMaster.DocumentService.Core.Authentication;

namespace TaskMaster.DocumentService.Data.Services;

/// <summary>
/// Service for validating API keys against the database.
/// </summary>
public class ApiKeyValidator : IApiKeyValidator
{
    private readonly DocumentDbContext _context;
    private readonly ILogger<ApiKeyValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiKeyValidator"/> class.
    /// </summary>
    public ApiKeyValidator(DocumentDbContext context, ILogger<ApiKeyValidator> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Validates an API key against the database.
    /// </summary>
    public async Task<ApiKeyValidationResult> ValidateAsync(string apiKey)
    {
        try
        {
            var keyHash = ApiKeyHasher.HashApiKey(apiKey);

            var apiKeyEntity = await _context.ApiKeys
                .Include(k => k.Tenant)
                .FirstOrDefaultAsync(k => k.KeyHash == keyHash && k.IsActive);

            if (apiKeyEntity == null)
            {
                _logger.LogWarning("API key not found or inactive");
                return ApiKeyValidationResult.Failure();
            }

            if (apiKeyEntity.ExpiresAt.HasValue && apiKeyEntity.ExpiresAt.Value < DateTime.UtcNow)
            {
                _logger.LogWarning("API key has expired for tenant {TenantId}", apiKeyEntity.TenantId);
                return ApiKeyValidationResult.Failure();
            }

            if (!apiKeyEntity.Tenant.IsActive)
            {
                _logger.LogWarning("Tenant {TenantId} is not active", apiKeyEntity.TenantId);
                return ApiKeyValidationResult.Failure();
            }

            // Update last used timestamp (fire and forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    apiKeyEntity.LastUsedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update API key last used timestamp");
                }
            });

            return ApiKeyValidationResult.Success(apiKeyEntity.TenantId, apiKeyEntity.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating API key");
            return ApiKeyValidationResult.Failure();
        }
    }
}
