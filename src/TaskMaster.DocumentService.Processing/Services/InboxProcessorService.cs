using System.Text.Json;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskMaster.DocumentService.Core.Interfaces;
using TaskMaster.DocumentService.Processing.Configuration;
using TaskMaster.DocumentService.Processing.Interfaces;
using TaskMaster.DocumentService.Processing.Models;

namespace TaskMaster.DocumentService.Processing.Services;

/// <summary>
/// Implementation of inbox processor service for processing files dropped in blob storage.
/// Implements the dump-and-index pattern for document ingestion.
/// </summary>
public class InboxProcessorService : IInboxProcessorService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly IDocumentService _documentService;
    private readonly ILogger<InboxProcessorService> _logger;
    private readonly InboxProcessorOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="InboxProcessorService"/> class.
    /// </summary>
    /// <param name="blobServiceClient">The Azure Blob Service client.</param>
    /// <param name="documentService">The document service.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The inbox processor configuration options.</param>
    public InboxProcessorService(
        BlobServiceClient blobServiceClient,
        IDocumentService documentService,
        ILogger<InboxProcessorService> logger,
        IOptions<InboxProcessorOptions> options)
    {
        _blobServiceClient = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));
        _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc/>
    public async Task<int> ProcessInboxFilesAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogDebug("Inbox processor is disabled");
            return 0;
        }

        try
        {
            _logger.LogInformation("Starting inbox file processing");

            var inboxContainer = _blobServiceClient.GetBlobContainerClient(_options.InboxContainerName);

            // Ensure containers exist
            await inboxContainer.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            var processedContainer = _blobServiceClient.GetBlobContainerClient(_options.ProcessedContainerName);
            await processedContainer.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            var failedContainer = _blobServiceClient.GetBlobContainerClient(_options.FailedContainerName);
            await failedContainer.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            // Get list of files in inbox
            var blobItems = new List<BlobItem>();
            await foreach (var blobItem in inboxContainer.GetBlobsAsync(cancellationToken: cancellationToken))
            {
                blobItems.Add(blobItem);

                if (blobItems.Count >= _options.BatchSize)
                {
                    break;
                }
            }

            if (blobItems.Count == 0)
            {
                _logger.LogDebug("No files found in inbox");
                return 0;
            }

            _logger.LogInformation("Found {Count} files in inbox to process", blobItems.Count);

            var processedCount = 0;

            foreach (var blobItem in blobItems)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Processing cancelled, processed {Count} files", processedCount);
                    break;
                }

                var success = await ProcessFileAsync(
                    inboxContainer,
                    processedContainer,
                    failedContainer,
                    blobItem,
                    cancellationToken);

                if (success)
                {
                    processedCount++;
                }
            }

            _logger.LogInformation(
                "Inbox processing completed. Processed {ProcessedCount} of {TotalCount} files",
                processedCount,
                blobItems.Count);

            return processedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during inbox file processing");
            throw;
        }
    }

    /// <summary>
    /// Processes a single file from the inbox.
    /// </summary>
    /// <param name="inboxContainer">The inbox container client.</param>
    /// <param name="processedContainer">The processed container client.</param>
    /// <param name="failedContainer">The failed container client.</param>
    /// <param name="blobItem">The blob item to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if processing was successful, false otherwise.</returns>
    private async Task<bool> ProcessFileAsync(
        BlobContainerClient inboxContainer,
        BlobContainerClient processedContainer,
        BlobContainerClient failedContainer,
        BlobItem blobItem,
        CancellationToken cancellationToken)
    {
        var blobName = blobItem.Name;
        _logger.LogInformation("Processing file: {BlobName}", blobName);

        try
        {
            var blobClient = inboxContainer.GetBlobClient(blobName);

            // Get blob properties and metadata
            var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
            var metadata = ExtractMetadata(blobName, properties.Value, blobItem);

            // Download blob content
            var downloadResponse = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
            var content = downloadResponse.Value.Content;

            // Create document using DocumentService
            var document = await _documentService.CreateDocumentAsync(
                tenantId: metadata.TenantId,
                documentTypeId: metadata.DocumentTypeId,
                title: metadata.Title,
                description: metadata.Description,
                content: content,
                fileName: metadata.FileName,
                contentType: metadata.ContentType,
                metadata: metadata.Metadata,
                tags: metadata.Tags,
                createdBy: _options.SystemUser,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Successfully created document {DocumentId} from file {BlobName}",
                document.Id,
                blobName);

            // Move file to processed container
            await MoveFileAsync(
                blobClient,
                processedContainer,
                blobName,
                properties.Value.Metadata,
                cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process file {BlobName}", blobName);

            try
            {
                // Move file to failed container
                var blobClient = inboxContainer.GetBlobClient(blobName);
                var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);

                // Add error information to metadata
                var metadata = new Dictionary<string, string>(properties.Value.Metadata)
                {
                    ["ErrorMessage"] = ex.Message,
                    ["ErrorTime"] = DateTime.UtcNow.ToString("o"),
                    ["ProcessedBy"] = _options.SystemUser
                };

                await MoveFileAsync(
                    blobClient,
                    failedContainer,
                    blobName,
                    metadata,
                    cancellationToken);
            }
            catch (Exception moveEx)
            {
                _logger.LogError(moveEx, "Failed to move file {BlobName} to failed container", blobName);
            }

            return false;
        }
    }

    /// <summary>
    /// Extracts metadata from blob name, properties, and item.
    /// </summary>
    /// <param name="blobName">The blob name.</param>
    /// <param name="properties">The blob properties.</param>
    /// <param name="blobItem">The blob item.</param>
    /// <returns>The extracted inbox file metadata.</returns>
    private InboxFileMetadata ExtractMetadata(
        string blobName,
        BlobProperties properties,
        BlobItem blobItem)
    {
        var metadata = new InboxFileMetadata
        {
            BlobName = blobName,
            FileName = Path.GetFileName(blobName),
            ContentType = properties.ContentType ?? "application/octet-stream",
            TenantId = _options.DefaultTenantId,
            DocumentTypeId = _options.DefaultDocumentTypeId,
            Title = Path.GetFileNameWithoutExtension(blobName)
        };

        // Extract metadata from blob metadata tags
        if (properties.Metadata != null)
        {
            if (properties.Metadata.TryGetValue("TenantId", out var tenantIdStr) &&
                int.TryParse(tenantIdStr, out var tenantId))
            {
                metadata.TenantId = tenantId;
            }

            if (properties.Metadata.TryGetValue("DocumentTypeId", out var docTypeIdStr) &&
                int.TryParse(docTypeIdStr, out var documentTypeId))
            {
                metadata.DocumentTypeId = documentTypeId;
            }

            if (properties.Metadata.TryGetValue("Title", out var title))
            {
                metadata.Title = title;
            }

            if (properties.Metadata.TryGetValue("Description", out var description))
            {
                metadata.Description = description;
            }

            if (properties.Metadata.TryGetValue("Metadata", out var metadataJson))
            {
                metadata.Metadata = metadataJson;
            }

            if (properties.Metadata.TryGetValue("Tags", out var tags))
            {
                metadata.Tags = tags;
            }
        }

        // Parse blob name for tenant folder structure (e.g., "tenant-{id}/filename.ext")
        var parts = blobName.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 1 && parts[0].StartsWith("tenant-", StringComparison.OrdinalIgnoreCase))
        {
            var tenantIdPart = parts[0].Substring(7); // Remove "tenant-" prefix
            if (int.TryParse(tenantIdPart, out var tenantId))
            {
                metadata.TenantId = tenantId;
            }
        }

        _logger.LogDebug(
            "Extracted metadata for {BlobName}: TenantId={TenantId}, DocumentTypeId={DocumentTypeId}, Title={Title}",
            blobName,
            metadata.TenantId,
            metadata.DocumentTypeId,
            metadata.Title);

        return metadata;
    }

    /// <summary>
    /// Moves a file from the inbox to another container.
    /// </summary>
    /// <param name="sourceBlobClient">The source blob client.</param>
    /// <param name="destinationContainer">The destination container client.</param>
    /// <param name="blobName">The blob name.</param>
    /// <param name="metadata">The metadata to set on the destination blob.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task MoveFileAsync(
        BlobClient sourceBlobClient,
        BlobContainerClient destinationContainer,
        string blobName,
        IDictionary<string, string> metadata,
        CancellationToken cancellationToken)
    {
        try
        {
            // Add processing metadata
            var enrichedMetadata = new Dictionary<string, string>(metadata)
            {
                ["ProcessedTime"] = DateTime.UtcNow.ToString("o"),
                ["ProcessedBy"] = _options.SystemUser,
                ["SourceContainer"] = _options.InboxContainerName
            };

            // Create destination blob with timestamp to avoid conflicts
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var destinationBlobName = $"{timestamp}_{blobName.Replace("/", "_")}";
            var destinationBlobClient = destinationContainer.GetBlobClient(destinationBlobName);

            // Copy blob to destination
            var copyOperation = await destinationBlobClient.StartCopyFromUriAsync(
                sourceBlobClient.Uri,
                cancellationToken: cancellationToken);

            await copyOperation.WaitForCompletionAsync(cancellationToken);

            // Set metadata on destination blob
            await destinationBlobClient.SetMetadataAsync(enrichedMetadata, cancellationToken: cancellationToken);

            // Delete source blob
            await sourceBlobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Moved file {SourceBlob} to {DestinationContainer}/{DestinationBlob}",
                blobName,
                destinationContainer.Name,
                destinationBlobName);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(
                ex,
                "Failed to move file {BlobName} to container {DestinationContainer}",
                blobName,
                destinationContainer.Name);
            throw;
        }
    }
}
