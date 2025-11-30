using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskMaster.DocumentService.Core.Interfaces;
using TaskMaster.DocumentService.Data.Repositories;

namespace TaskMaster.DocumentService.Data;

/// <summary>
/// Extension methods for configuring data services in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Document Service data layer services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDocumentServiceData(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register DbContext
        services.AddDbContext<DocumentServiceDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DocumentServiceDb")
                ?? throw new InvalidOperationException("Connection string 'DocumentServiceDb' not found.");

            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
                sqlOptions.CommandTimeout(60);
            });

            // Enable sensitive data logging only in development
            var enableSensitiveLogging = configuration["Logging:EnableSensitiveDataLogging"];
            if (bool.TryParse(enableSensitiveLogging, out var shouldLog) && shouldLog)
            {
                options.EnableSensitiveDataLogging();
            }
        });

        // Register repositories
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IDocumentTypeRepository, DocumentTypeRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();

        // Register Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
