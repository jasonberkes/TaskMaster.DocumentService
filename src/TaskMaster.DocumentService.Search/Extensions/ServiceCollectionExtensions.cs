using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskMaster.DocumentService.Core.Interfaces;
using TaskMaster.DocumentService.Search.BackgroundServices;
using TaskMaster.DocumentService.Search.Configuration;
using TaskMaster.DocumentService.Search.HealthChecks;
using TaskMaster.DocumentService.Search.Interfaces;
using TaskMaster.DocumentService.Search.Services;

namespace TaskMaster.DocumentService.Search.Extensions;

/// <summary>
/// Extension methods for configuring search services in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Document Service search capabilities using Meilisearch.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDocumentServiceSearch(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        // Configure Meilisearch options
        services.Configure<MeilisearchOptions>(
            configuration.GetSection(MeilisearchOptions.SectionName));

        // Register search service
        services.AddSingleton<ISearchService, MeilisearchService>();
        
        // Register document indexer for Core layer to use
        services.AddSingleton<IDocumentIndexer, DocumentIndexer>();

        // Register health check
        services.AddHealthChecks()
            .AddCheck<MeilisearchHealthCheck>("meilisearch");

        // Register background indexing service
        services.AddHostedService<DocumentIndexingBackgroundService>();

        return services;
    }

    /// <summary>
    /// Adds the Document Service search capabilities using Meilisearch with custom options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure Meilisearch options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDocumentServiceSearch(
        this IServiceCollection services,
        Action<MeilisearchOptions> configureOptions)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configureOptions == null)
        {
            throw new ArgumentNullException(nameof(configureOptions));
        }

        // Configure Meilisearch options
        services.Configure(configureOptions);

        // Register search service
        services.AddSingleton<ISearchService, MeilisearchService>();
        
        // Register document indexer for Core layer to use
        services.AddSingleton<IDocumentIndexer, DocumentIndexer>();

        // Register health check
        services.AddHealthChecks()
            .AddCheck<MeilisearchHealthCheck>("meilisearch");

        // Register background indexing service
        services.AddHostedService<DocumentIndexingBackgroundService>();

        return services;
    }
}
