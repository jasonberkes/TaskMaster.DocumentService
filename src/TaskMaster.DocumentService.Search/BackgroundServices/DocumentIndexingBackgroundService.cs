using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskMaster.DocumentService.Core.Interfaces;
using TaskMaster.DocumentService.Search.Configuration;
using TaskMaster.DocumentService.Search.Interfaces;

namespace TaskMaster.DocumentService.Search.BackgroundServices;

/// <summary>
/// Background service that periodically indexes documents that were missed or failed during initial indexing.
/// Provides a safety net for ensuring all documents are searchable.
/// </summary>
public class DocumentIndexingBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DocumentIndexingBackgroundService> _logger;
    private readonly MeilisearchOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentIndexingBackgroundService"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for creating scoped services.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The Meilisearch configuration options.</param>
    public DocumentIndexingBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<DocumentIndexingBackgroundService> logger,
        IOptions<MeilisearchOptions> options)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.BackgroundIndexingEnabled)
        {
            _logger.LogInformation("Background document indexing service is disabled");
            return;
        }

        _logger.LogInformation(
            "Background document indexing service starting. Polling interval: {PollingIntervalMinutes} minutes",
            _options.BackgroundIndexingIntervalMinutes);

        // Wait before starting to allow the application to fully initialize
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogDebug("Background indexing cycle starting");

                using var scope = _serviceProvider.CreateScope();
                var indexedCount = await ProcessUnindexedDocumentsAsync(scope.ServiceProvider, stoppingToken);

                if (indexedCount > 0)
                {
                    _logger.LogInformation("Background indexing completed. Indexed {Count} documents", indexedCount);
                }
                else
                {
                    _logger.LogDebug("No documents requiring indexing found");
                }

                await Task.Delay(
                    TimeSpan.FromMinutes(_options.BackgroundIndexingIntervalMinutes),
                    stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Background document indexing service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in background document indexing service");

                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        _logger.LogInformation("Background document indexing service stopped");
    }

    private async Task<int> ProcessUnindexedDocumentsAsync(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var unitOfWork = serviceProvider.GetRequiredService<IUnitOfWork>();
        var searchService = serviceProvider.GetRequiredService<ISearchService>();

        // Get documents that need indexing
        var documents = (await unitOfWork.Documents.GetDocumentsNeedingIndexingAsync(cancellationToken)).ToList();

        if (!documents.Any())
        {
            return 0;
        }

        _logger.LogInformation("Found {Count} documents needing indexing", documents.Count);

        var indexedCount = 0;
        var batchSize = _options.BatchSize;

        // Process in batches
        for (var i = 0; i < documents.Count; i += batchSize)
        {
            var batch = documents.Skip(i).Take(batchSize).ToList();

            try
            {
                var results = await searchService.IndexDocumentsBatchAsync(batch, cancellationToken);

                // Update MeilisearchId and LastIndexedAt for successfully indexed documents
                foreach (var document in batch)
                {
                    if (results.TryGetValue(document.Id, out var meilisearchId))
                    {
                        document.MeilisearchId = meilisearchId;
                        document.LastIndexedAt = DateTime.UtcNow;
                        unitOfWork.Documents.Update(document);
                        indexedCount++;
                    }
                }

                await unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogDebug("Batch indexed {Count} documents", batch.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to index batch of {Count} documents", batch.Count);
            }
        }

        return indexedCount;
    }

    /// <inheritdoc/>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Background document indexing service is stopping gracefully");
        await base.StopAsync(cancellationToken);
    }
}
