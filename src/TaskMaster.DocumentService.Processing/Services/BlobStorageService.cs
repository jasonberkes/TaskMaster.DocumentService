using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskMaster.DocumentService.Core.Models;
using TaskMaster.DocumentService.Processing.Configuration;
using TaskMaster.DocumentService.Processing.Interfaces;

namespace TaskMaster.DocumentService.Processing.Services;

/// <summary>
/// Service for managing blob storage operations for document processing.
/// </summary>
public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly BlobStorageOptions _options;
    private readonly ILogger<BlobStorageService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobStorageService"/> class.
    /// </summary>
    /// <param name="blobServiceClient">The blob service client.</param>
    /// <param name="options">The blob storage options.</param>
    /// <param name="logger">The logger.</param>
    public BlobStorageService(
        BlobServiceClient blobServiceClient,
        IOptions<BlobStorageOptions> options,
        ILogger<BlobStorageService> logger)
    {
        _blobServiceClient = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<InboxDocument>> ListInboxDocumentsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_options.InboxContainerName);
            await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            var inboxDocuments = new List<InboxDocument>();

            await foreach (var blobItem in containerClient.GetBlobsAsync(cancellationToken: cancellationToken))
            {
                var blobClient = containerClient.GetBlobClient(blobItem.Name);
                var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);

                var inboxDoc = new InboxDocument
                {
                    BlobName = blobItem.Name,
                    BlobUri = blobClient.Uri,
                    ContentType = properties.Value.ContentType,
                    ContentLength = properties.Value.ContentLength,
                    CreatedOn = properties.Value.CreatedOn,
                    TenantId = ExtractTenantIdFromMetadata(properties.Value.Metadata),
                    DocumentTypeId = ExtractDocumentTypeIdFromMetadata(properties.Value.Metadata)
                };

                inboxDocuments.Add(inboxDoc);
            }

            _logger.LogInformation("Found {Count} documents in inbox", inboxDocuments.Count);
            return inboxDocuments;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing inbox documents");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<InboxDocument?> DownloadInboxDocumentAsync(string blobName, CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_options.InboxContainerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                _logger.LogWarning("Blob {BlobName} not found in inbox", blobName);
                return null;
            }

            var download = await blobClient.DownloadAsync(cancellationToken);
            var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);

            var inboxDoc = new InboxDocument
            {
                BlobName = blobName,
                BlobUri = blobClient.Uri,
                ContentStream = download.Value.Content,
                ContentType = properties.Value.ContentType,
                ContentLength = properties.Value.ContentLength,
                CreatedOn = properties.Value.CreatedOn,
                TenantId = ExtractTenantIdFromMetadata(properties.Value.Metadata),
                DocumentTypeId = ExtractDocumentTypeIdFromMetadata(properties.Value.Metadata)
            };

            _logger.LogInformation("Downloaded blob {BlobName} from inbox", blobName);
            return inboxDoc;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading blob {BlobName} from inbox", blobName);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task MoveToProcessedAsync(string blobName, CancellationToken cancellationToken = default)
    {
        try
        {
            var inboxContainerClient = _blobServiceClient.GetBlobContainerClient(_options.InboxContainerName);
            var processedContainerClient = _blobServiceClient.GetBlobContainerClient(_options.ProcessedContainerName);
            await processedContainerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            var sourceBlobClient = inboxContainerClient.GetBlobClient(blobName);
            var destBlobClient = processedContainerClient.GetBlobClient(blobName);

            // Copy to processed
            await destBlobClient.StartCopyFromUriAsync(sourceBlobClient.Uri, cancellationToken: cancellationToken);

            // Wait for copy to complete
            var properties = await destBlobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
            while (properties.Value.CopyStatus == CopyStatus.Pending)
            {
                await Task.Delay(100, cancellationToken);
                properties = await destBlobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
            }

            // Delete from inbox
            await sourceBlobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);

            _logger.LogInformation("Moved blob {BlobName} to processed container", blobName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving blob {BlobName} to processed", blobName);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task MoveToFailedAsync(string blobName, string errorMessage, CancellationToken cancellationToken = default)
    {
        try
        {
            var inboxContainerClient = _blobServiceClient.GetBlobContainerClient(_options.InboxContainerName);
            var failedContainerClient = _blobServiceClient.GetBlobContainerClient(_options.FailedContainerName);
            await failedContainerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            var sourceBlobClient = inboxContainerClient.GetBlobClient(blobName);
            var destBlobClient = failedContainerClient.GetBlobClient(blobName);

            // Copy to failed with error metadata
            await destBlobClient.StartCopyFromUriAsync(sourceBlobClient.Uri, cancellationToken: cancellationToken);

            // Wait for copy to complete
            var properties = await destBlobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
            while (properties.Value.CopyStatus == CopyStatus.Pending)
            {
                await Task.Delay(100, cancellationToken);
                properties = await destBlobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
            }

            // Add error metadata
            var metadata = new Dictionary<string, string>
            {
                { "ErrorMessage", errorMessage },
                { "FailedAt", DateTime.UtcNow.ToString("O") }
            };
            await destBlobClient.SetMetadataAsync(metadata, cancellationToken: cancellationToken);

            // Delete from inbox
            await sourceBlobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);

            _logger.LogWarning("Moved blob {BlobName} to failed container: {ErrorMessage}", blobName, errorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving blob {BlobName} to failed", blobName);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task UploadDocumentAsync(Stream stream, string blobPath, string contentType, CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_options.DocumentsContainerName);
            await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            var blobClient = containerClient.GetBlobClient(blobPath);

            stream.Position = 0;
            await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: cancellationToken);

            _logger.LogInformation("Uploaded document to {BlobPath}", blobPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document to {BlobPath}", blobPath);
            throw;
        }
    }

    private int ExtractTenantIdFromMetadata(IDictionary<string, string> metadata)
    {
        if (metadata.TryGetValue("TenantId", out var tenantIdStr) && int.TryParse(tenantIdStr, out var tenantId))
        {
            return tenantId;
        }
        return _options.DefaultTenantId;
    }

    private int ExtractDocumentTypeIdFromMetadata(IDictionary<string, string> metadata)
    {
        if (metadata.TryGetValue("DocumentTypeId", out var docTypeIdStr) && int.TryParse(docTypeIdStr, out var docTypeId))
        {
            return docTypeId;
        }
        return _options.DefaultDocumentTypeId;
    }
}
