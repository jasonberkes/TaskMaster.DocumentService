using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskMaster.DocumentService.SDK.Clients;
using TaskMaster.DocumentService.SDK.Configuration;
using TaskMaster.DocumentService.SDK.Interfaces;

namespace TaskMaster.DocumentService.SDK.Extensions;

/// <summary>
/// Extension methods for configuring Document Service SDK in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Document Service SDK to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration section containing DocumentServiceOptions.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDocumentServiceClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DocumentServiceOptions>(configuration);

        var options = configuration.Get<DocumentServiceOptions>();
        if (options == null)
        {
            throw new InvalidOperationException("DocumentServiceOptions configuration is missing.");
        }

        return AddDocumentServiceClient(services, options);
    }

    /// <summary>
    /// Adds the Document Service SDK to the service collection with options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure the options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDocumentServiceClient(
        this IServiceCollection services,
        Action<DocumentServiceOptions> configureOptions)
    {
        var options = new DocumentServiceOptions();
        configureOptions(options);

        services.Configure(configureOptions);

        return AddDocumentServiceClient(services, options);
    }

    /// <summary>
    /// Adds the Document Service SDK to the service collection with options instance.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The configuration options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDocumentServiceClient(
        this IServiceCollection services,
        DocumentServiceOptions options)
    {
        if (string.IsNullOrEmpty(options.BaseUrl))
        {
            throw new ArgumentException("BaseUrl must be provided in DocumentServiceOptions.", nameof(options));
        }

        services.AddHttpClient<IDocumentServiceClient, DocumentServiceClient>(client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);

            if (!string.IsNullOrEmpty(options.ApiKey))
            {
                client.DefaultRequestHeaders.Add("X-API-Key", options.ApiKey);
            }
        });

        // Register individual clients for direct injection if needed
        services.AddScoped<IDocumentsClient>(sp =>
        {
            var client = sp.GetRequiredService<IDocumentServiceClient>();
            return client.Documents;
        });

        services.AddScoped<IDocumentTypesClient>(sp =>
        {
            var client = sp.GetRequiredService<IDocumentServiceClient>();
            return client.DocumentTypes;
        });

        services.AddScoped<ITenantsClient>(sp =>
        {
            var client = sp.GetRequiredService<IDocumentServiceClient>();
            return client.Tenants;
        });

        return services;
    }
}
