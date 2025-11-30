using Microsoft.Extensions.Diagnostics.HealthChecks;
using TaskMaster.DocumentService.Search.Services;

namespace TaskMaster.DocumentService.Api.Services;

/// <summary>
/// Health check for Meilisearch service availability.
/// </summary>
public class MeilisearchHealthCheck : IHealthCheck
{
    private readonly IMeilisearchService _meilisearchService;
    private readonly ILogger<MeilisearchHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MeilisearchHealthCheck"/> class.
    /// </summary>
    /// <param name="meilisearchService">Meilisearch service.</param>
    /// <param name="logger">Logger instance.</param>
    public MeilisearchHealthCheck(IMeilisearchService meilisearchService, ILogger<MeilisearchHealthCheck> logger)
    {
        _meilisearchService = meilisearchService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var isHealthy = await _meilisearchService.HealthCheckAsync(cancellationToken);

            if (isHealthy)
            {
                return HealthCheckResult.Healthy("Meilisearch is available");
            }

            _logger.LogWarning("Meilisearch health check returned unhealthy status");
            return HealthCheckResult.Unhealthy("Meilisearch is not available");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Meilisearch health check failed with exception");
            return HealthCheckResult.Unhealthy("Meilisearch health check failed", ex);
        }
    }
}
