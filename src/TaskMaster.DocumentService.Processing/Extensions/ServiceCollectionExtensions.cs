using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskMaster.DocumentService.Processing.BackgroundServices;
using TaskMaster.DocumentService.Processing.Configuration;
using TaskMaster.DocumentService.Processing.Interfaces;
using TaskMaster.DocumentService.Processing.Services;

namespace TaskMaster.DocumentService.Processing.Extensions;

/// <summary>
/// Extension methods for configuring document processing services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds document processing services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDocumentServiceProcessing(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        // Configure inbox processor options
        services.Configure<InboxProcessorOptions>(
            configuration.GetSection(InboxProcessorOptions.SectionName));

        // Register inbox processor service
        services.AddScoped<IInboxProcessorService, InboxProcessorService>();

        // Register background service
        services.AddHostedService<InboxProcessorBackgroundService>();

        // Configure code review migration options
        services.Configure<CodeReviewMigrationOptions>(
            configuration.GetSection("CodeReviewMigration"));

        // Register code review migration service
        services.AddScoped<ICodeReviewMigrationService, CodeReviewMigrationService>();

        return services;
    }
}
