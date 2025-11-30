using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskMaster.DocumentService.Processing.Configuration;
using TaskMaster.DocumentService.Processing.Interfaces;

namespace TaskMaster.DocumentService.Processing.BackgroundServices;

/// <summary>
/// Background service that periodically processes files from the inbox blob storage container.
/// Implements the dump-and-index pattern for continuous document ingestion.
/// </summary>
public class InboxProcessorBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InboxProcessorBackgroundService> _logger;
    private readonly InboxProcessorOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="InboxProcessorBackgroundService"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for creating scoped services.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The inbox processor configuration options.</param>
    public InboxProcessorBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<InboxProcessorBackgroundService> logger,
        IOptions<InboxProcessorOptions> options)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Inbox processor background service is disabled");
            return;
        }

        _logger.LogInformation(
            "Inbox processor background service starting. Polling interval: {PollingIntervalSeconds} seconds",
            _options.PollingIntervalSeconds);

        // Wait a short time before starting to allow the application to fully initialize
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogDebug("Inbox processor polling cycle starting");

                // Create a scope for the processing operation to ensure proper disposal of scoped services
                using (var scope = _serviceProvider.CreateScope())
                {
                    var inboxProcessorService = scope.ServiceProvider.GetRequiredService<IInboxProcessorService>();

                    var processedCount = await inboxProcessorService.ProcessInboxFilesAsync(stoppingToken);

                    if (processedCount > 0)
                    {
                        _logger.LogInformation("Processed {Count} files from inbox", processedCount);
                    }
                }

                _logger.LogDebug("Inbox processor polling cycle completed");

                // Wait for the next polling interval
                await Task.Delay(
                    TimeSpan.FromSeconds(_options.PollingIntervalSeconds),
                    stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // This is expected when the service is stopping
                _logger.LogInformation("Inbox processor background service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error occurred in inbox processor background service. Waiting {PollingIntervalSeconds} seconds before retry",
                    _options.PollingIntervalSeconds);

                // Wait before retrying to avoid tight loop on persistent errors
                try
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(_options.PollingIntervalSeconds),
                        stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Service is stopping
                    break;
                }
            }
        }

        _logger.LogInformation("Inbox processor background service stopped");
    }

    /// <inheritdoc/>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Inbox processor background service is stopping gracefully");
        await base.StopAsync(cancellationToken);
    }
}
