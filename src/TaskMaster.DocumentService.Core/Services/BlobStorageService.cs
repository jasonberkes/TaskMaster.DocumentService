using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskMaster.DocumentService.Core.Interfaces;

namespace TaskMaster.DocumentService.Core.Services;

/// <summary>
/// Implementation of blob storage service for document management.
/// </summary>
public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<BlobStorageService> _logger;
    private readonly BlobStorageOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobStorageService"/> class.
    /// </summary>
    /// <param name="blobServiceClient">The Azure Blob Service client.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The blob storage configuration options.</param>
    public BlobStorageService(
        BlobServiceClient blobServiceClient,
        ILogger<BlobStorageService> logger,
        IOptions<BlobStorageOptions> options)
    {
        _blobServiceClient = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc/>
    public async Task<string> UploadAsync(
        string containerName,
        string blobName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(containerName))
            throw new ArgumentException("Container name cannot be null or empty.", nameof(containerName));

        if (string.IsNullOrWhiteSpace(blobName))
            throw new ArgumentException("Blob name cannot be null or empty.", nameof(blobName));

        if (content == null)
            throw new ArgumentNullException(nameof(content));

        try
        {
            _logger.LogInformation("Uploading blob {BlobName} to container {ContainerName}", blobName, containerName);

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            var blobClient = containerClient.GetBlobClient(blobName);

            var blobHttpHeaders = new BlobHttpHeaders
            {
                ContentType = contentType
            };

            await blobClient.UploadAsync(
                content,
                new BlobUploadOptions
                {
                    HttpHeaders = blobHttpHeaders
                },
                cancellationToken);

            _logger.LogInformation("Successfully uploaded blob {BlobName} to container {ContainerName}", blobName, containerName);

            return blobClient.Uri.ToString();
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to upload blob {BlobName} to container {ContainerName}", blobName, containerName);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Stream> DownloadAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(containerName))
            throw new ArgumentException("Container name cannot be null or empty.", nameof(containerName));

        if (string.IsNullOrWhiteSpace(blobName))
            throw new ArgumentException("Blob name cannot be null or empty.", nameof(blobName));

        try
        {
            _logger.LogInformation("Downloading blob {BlobName} from container {ContainerName}", blobName, containerName);

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);

            _logger.LogInformation("Successfully downloaded blob {BlobName} from container {ContainerName}", blobName, containerName);

            return response.Value.Content;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Blob {BlobName} not found in container {ContainerName}", blobName, containerName);
            throw new FileNotFoundException($"Blob '{blobName}' not found in container '{containerName}'.", blobName);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to download blob {BlobName} from container {ContainerName}", blobName, containerName);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(containerName))
            throw new ArgumentException("Container name cannot be null or empty.", nameof(containerName));

        if (string.IsNullOrWhiteSpace(blobName))
            throw new ArgumentException("Blob name cannot be null or empty.", nameof(blobName));

        try
        {
            _logger.LogInformation("Deleting blob {BlobName} from container {ContainerName}", blobName, containerName);

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var response = await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);

            if (response.Value)
            {
                _logger.LogInformation("Successfully deleted blob {BlobName} from container {ContainerName}", blobName, containerName);
            }
            else
            {
                _logger.LogWarning("Blob {BlobName} not found in container {ContainerName} for deletion", blobName, containerName);
            }

            return response.Value;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to delete blob {BlobName} from container {ContainerName}", blobName, containerName);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(containerName))
            throw new ArgumentException("Container name cannot be null or empty.", nameof(containerName));

        if (string.IsNullOrWhiteSpace(blobName))
            throw new ArgumentException("Blob name cannot be null or empty.", nameof(blobName));

        try
        {
            _logger.LogDebug("Checking if blob {BlobName} exists in container {ContainerName}", blobName, containerName);

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var response = await blobClient.ExistsAsync(cancellationToken);

            return response.Value;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to check existence of blob {BlobName} in container {ContainerName}", blobName, containerName);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string> GetSasUriAsync(
        string containerName,
        string blobName,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(containerName))
            throw new ArgumentException("Container name cannot be null or empty.", nameof(containerName));

        if (string.IsNullOrWhiteSpace(blobName))
            throw new ArgumentException("Blob name cannot be null or empty.", nameof(blobName));

        if (expiresIn <= TimeSpan.Zero)
            throw new ArgumentException("Expiration duration must be greater than zero.", nameof(expiresIn));

        try
        {
            _logger.LogInformation("Generating SAS URI for blob {BlobName} in container {ContainerName}", blobName, containerName);

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            // Check if blob exists
            var exists = await blobClient.ExistsAsync(cancellationToken);
            if (!exists.Value)
            {
                throw new FileNotFoundException($"Blob '{blobName}' not found in container '{containerName}'.", blobName);
            }

            // Generate SAS token
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerName,
                BlobName = blobName,
                Resource = "b", // b = blob
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5), // Allow 5 minutes clock skew
                ExpiresOn = DateTimeOffset.UtcNow.Add(expiresIn)
            };

            // Set read permissions
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var sasUri = blobClient.GenerateSasUri(sasBuilder);

            _logger.LogInformation("Successfully generated SAS URI for blob {BlobName} in container {ContainerName}", blobName, containerName);

            return sasUri.ToString();
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to generate SAS URI for blob {BlobName} in container {ContainerName}", blobName, containerName);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<string>> ListBlobsAsync(
        string containerName,
        string? prefix = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(containerName))
            throw new ArgumentException("Container name cannot be null or empty.", nameof(containerName));

        try
        {
            _logger.LogInformation("Listing blobs in container {ContainerName} with prefix {Prefix}", containerName, prefix ?? "(none)");

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            // Check if container exists
            var exists = await containerClient.ExistsAsync(cancellationToken);
            if (!exists.Value)
            {
                _logger.LogWarning("Container {ContainerName} does not exist", containerName);
                return Enumerable.Empty<string>();
            }

            var blobNames = new List<string>();

            await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken))
            {
                blobNames.Add(blobItem.Name);
            }

            _logger.LogInformation("Found {Count} blobs in container {ContainerName}", blobNames.Count, containerName);

            return blobNames;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to list blobs in container {ContainerName}", containerName);
            throw;
        }
    }
}
