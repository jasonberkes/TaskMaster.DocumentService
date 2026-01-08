using System.Diagnostics;
using Azure.Storage.Blobs;
using Azure.Identity;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using TaskMaster.DocumentService.Api.Configuration;
using TaskMaster.DocumentService.Data;
using TaskMaster.DocumentService.Search.Interfaces;
using TaskMaster.DocumentService.Search.Services;

namespace TaskMaster.DocumentService.Api.Services;

/// <summary>
/// Background service that indexes blob metadata into Meilisearch
/// WI #976: PHASE 1: Meilisearch Container + Basic Indexer
/// WI #3660: Moved from Platform to DocumentService
/// </summary>
public class BlobIndexerJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BlobIndexerJob> _logger;
    private readonly BlobIndexerOptions _indexerOptions;
    private readonly TimeSpan _interval;
    private readonly TimeSpan _startupDelay;
    private readonly BlobServiceClient _blobServiceClient;

    public BlobIndexerJob(
        IServiceScopeFactory scopeFactory,
        ILogger<BlobIndexerJob> logger,
        IOptions<BlobIndexerOptions> indexerOptions,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _indexerOptions = indexerOptions.Value;
        _interval = TimeSpan.FromMinutes(_indexerOptions.IndexIntervalMinutes);
        _startupDelay = TimeSpan.FromSeconds(_indexerOptions.StartupDelaySeconds);

        // Initialize blob service client with Managed Identity
        var storageAccountName = configuration["Azure:StorageAccountName"] 
            ?? configuration["BlobStorage:AccountName"]
            ?? "tmprodeus2data";
        var credential = new DefaultAzureCredential();
        var blobServiceUri = new Uri($"https://{storageAccountName}.blob.core.windows.net");
        _blobServiceClient = new BlobServiceClient(blobServiceUri, credential);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_indexerOptions.Enabled)
        {
            _logger.LogInformation("🔍 Blob Indexer is disabled in configuration");
            return;
        }

        _logger.LogInformation("🔍 Blob Indexer Job starting (DocumentService)...");
        _logger.LogInformation("   - Interval: {Interval} minutes", _interval.TotalMinutes);
        _logger.LogInformation("   - Batch Size: {BatchSize} documents", _indexerOptions.BatchSize);

        await Task.Delay(_startupDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await IndexUnindexedDocumentsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in blob indexer job cycle");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task IndexUnindexedDocumentsAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DocumentServiceDbContext>();
        var meilisearchService = scope.ServiceProvider.GetRequiredService<ISearchService>();

        try
        {
            await meilisearchService.InitializeIndexAsync(cancellationToken);

            var blobMetadataToIndex = await dbContext.BlobMetadata
                .Where(bm => bm.IsActive && !bm.IsIndexed)
                .OrderBy(bm => bm.CreatedAt)
                .Take(_indexerOptions.BatchSize)
                .ToListAsync(cancellationToken);

            if (!blobMetadataToIndex.Any())
            {
                _logger.LogDebug("✅ No blob metadata to index");
                return;
            }

            _logger.LogInformation("📄 Found {Count} blob metadata entries to index", blobMetadataToIndex.Count);

            var blobsWithContent = 0;
            foreach (var blobMetadata in blobMetadataToIndex)
            {
                blobMetadata.TextContent = await DownloadBlobContentAsync(blobMetadata, cancellationToken);
                if (!string.IsNullOrEmpty(blobMetadata.TextContent))
                    blobsWithContent++;
            }

            // Index in Meilisearch
            await meilisearchService.IndexBlobMetadataAsync(blobMetadataToIndex, cancellationToken);

            // Update indexing status
            foreach (var blobMetadata in blobMetadataToIndex)
            {
                blobMetadata.IsIndexed = true;
                blobMetadata.LastIndexedAt = DateTime.UtcNow;
                blobMetadata.MeilisearchDocumentId = $"blob-{blobMetadata.Id}";
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation("✅ Indexed {Count} blob metadata entries ({WithContent} with content) in {Elapsed:F2}s",
                blobMetadataToIndex.Count, blobsWithContent, stopwatch.Elapsed.TotalSeconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to index documents after {Elapsed:F2}s", stopwatch.Elapsed.TotalSeconds);
        }
    }

    private async Task<string?> DownloadBlobContentAsync(
        Core.Entities.BlobMetadata blobMetadata,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!string.IsNullOrEmpty(blobMetadata.MimeType) && !IsTextFile(blobMetadata.MimeType))
                return null;

            var maxFileSize = _indexerOptions.MaxFileSizeMB * 1024 * 1024;
            if (blobMetadata.ContentLength.HasValue && blobMetadata.ContentLength.Value > maxFileSize)
                return null;

            var blobClient = _blobServiceClient
                .GetBlobContainerClient(blobMetadata.ContainerName)
                .GetBlobClient(blobMetadata.BlobPath);

            if (!await blobClient.ExistsAsync(cancellationToken))
                return null;

            var downloadResult = await blobClient.DownloadContentAsync(cancellationToken);
            return downloadResult.Value.Content.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to download blob content for {Title}", blobMetadata.Title);
            return null;
        }
    }

    private static bool IsTextFile(string contentType) =>
        contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) ||
        contentType.Equals("application/json", StringComparison.OrdinalIgnoreCase) ||
        contentType.Equals("application/xml", StringComparison.OrdinalIgnoreCase) ||
        contentType.Equals("application/markdown", StringComparison.OrdinalIgnoreCase);
}
