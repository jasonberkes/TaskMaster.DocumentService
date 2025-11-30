using Microsoft.Extensions.Diagnostics.HealthChecks;
using TaskMaster.DocumentService.Search.Interfaces;

namespace TaskMaster.DocumentService.Search.HealthChecks;

/// <summary>
/// Health check for Meilisearch availability.
/// </summary>
public class MeilisearchHealthCheck : IHealthCheck
{
    private readonly ISearchService _searchService;

    /// <summary>
    /// Initializes a new instance of the <see cref="MeilisearchHealthCheck"/> class.
    /// </summary>
    /// <param name="searchService">The search service.</param>
    public MeilisearchHealthCheck(ISearchService searchService)
    {
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
    }

    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var isHealthy = await _searchService.IsHealthyAsync(cancellationToken);

            return isHealthy
                ? HealthCheckResult.Healthy("Meilisearch is available and responding.")
                : HealthCheckResult.Unhealthy("Meilisearch is not responding correctly.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Meilisearch health check failed.",
                exception: ex);
        }
    }
}
