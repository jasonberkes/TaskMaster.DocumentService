using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskMaster.DocumentService.Processing.Configuration;
using TaskMaster.DocumentService.Processing.Interfaces;

namespace TaskMaster.DocumentService.Processing.BackgroundServices;

/// <summary>
/// Background service that continuously polls the inbox container for new documents
/// and processes them using the dump-and-index pattern.
/// </summary>
public class InboxProcessorBackgroundService : BackgroundService
{
    private readonly IBlobStorageService _blobStorageService;
    private readonly IDocumentProcessor _documentProcessor;
    private readonly InboxProcessorOptions _options;
    private readonly ILogger<InboxProcessorBackgroundService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InboxProcessorBackgroundService"/> class.
    /// </summary>
    /// <param name="blobStorageService">The blob storage service.</param>
    /// <param name="documentProcessor">The document processor.</param>
    /// <param name="options">The inbox processor options.</param>
    /// <param name="logger">The logger.</param>
    public InboxProcessorBackgroundService(
        IBlobStorageService blobStorageService,
        IDocumentProcessor documentProcessor,
        IOptions<InboxProcessorOptions> options,
        ILogger<InboxProcessorBackgroundService> logger)
    {
        _blobStorageService = blobStorageService ?? throw new ArgumentNullException(nameof(blobStorageService));
        _documentProcessor = documentProcessor ?? throw new ArgumentNullException(nameof(documentProcessor));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Inbox processor is disabled in configuration");
            return;
        }

        _logger.LogInformation(
            "Inbox processor background service starting. Polling interval: {IntervalSeconds}s, Batch size: {BatchSize}",
            _options.PollingIntervalSeconds,
            _options.BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessInboxAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing inbox");
            }

            // Wait for the configured polling interval
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_options.PollingIntervalSeconds), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Expected when stopping the service
                break;
            }
        }

        _logger.LogInformation("Inbox processor background service is stopping");
    }

    /// <summary>
    /// Processes all documents in the inbox container.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task ProcessInboxAsync(CancellationToken cancellationToken)
    {
        try
        {
            // List all documents in inbox
            var inboxDocuments = await _blobStorageService.ListInboxDocumentsAsync(cancellationToken);
            var documentsList = inboxDocuments.ToList();

            if (documentsList.Count == 0)
            {
                _logger.LogDebug("No documents found in inbox");
                return;
            }

            _logger.LogInformation("Found {Count} documents in inbox to process", documentsList.Count);

            // Process in batches
            var batches = documentsList
                .Select((doc, index) => new { doc, index })
                .GroupBy(x => x.index / _options.BatchSize)
                .Select(g => g.Select(x => x.doc).ToList())
                .ToList();

            foreach (var batch in batches)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                await ProcessBatchAsync(batch, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing inbox");
        }
    }

    /// <summary>
    /// Processes a batch of documents.
    /// </summary>
    /// <param name="batch">The batch of documents to process.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task ProcessBatchAsync(List<Core.Models.InboxDocument> batch, CancellationToken cancellationToken)
    {
        var tasks = batch.Select(doc => ProcessSingleDocumentAsync(doc.BlobName, cancellationToken));
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Processes a single document from the inbox.
    /// </summary>
    /// <param name="blobName">The blob name to process.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task ProcessSingleDocumentAsync(string blobName, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing document: {BlobName}", blobName);

            // Download the document
            var inboxDocument = await _blobStorageService.DownloadInboxDocumentAsync(blobName, cancellationToken);
            if (inboxDocument == null)
            {
                _logger.LogWarning("Document {BlobName} no longer exists in inbox", blobName);
                return;
            }

            // Process the document
            var result = await _documentProcessor.ProcessDocumentAsync(inboxDocument, cancellationToken);

            // Clean up the stream
            if (inboxDocument.ContentStream != null)
            {
                await inboxDocument.ContentStream.DisposeAsync();
            }

            // Move to appropriate container based on result
            if (result.Success)
            {
                await _blobStorageService.MoveToProcessedAsync(blobName, cancellationToken);
                _logger.LogInformation(
                    "Successfully processed document {BlobName} -> Document ID {DocumentId} in {ProcessingTimeMs}ms",
                    blobName,
                    result.DocumentId,
                    result.ProcessingTimeMs);
            }
            else
            {
                await _blobStorageService.MoveToFailedAsync(blobName, result.ErrorMessage ?? "Unknown error", cancellationToken);
                _logger.LogError(
                    "Failed to process document {BlobName}: {ErrorMessage}",
                    blobName,
                    result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document {BlobName}", blobName);

            try
            {
                await _blobStorageService.MoveToFailedAsync(blobName, ex.Message, cancellationToken);
            }
            catch (Exception moveEx)
            {
                _logger.LogError(moveEx, "Failed to move document {BlobName} to failed container", blobName);
            }
        }
    }
}
