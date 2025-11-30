namespace TaskMaster.DocumentService.Core.Interfaces;

/// <summary>
/// Service for managing document storage in Azure Blob Storage.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Uploads a document to blob storage.
    /// </summary>
    /// <param name="containerName">The name of the blob container.</param>
    /// <param name="blobName">The name of the blob.</param>
    /// <param name="content">The content stream to upload.</param>
    /// <param name="contentType">The content type of the document.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The URI of the uploaded blob.</returns>
    Task<string> UploadAsync(
        string containerName,
        string blobName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a document from blob storage.
    /// </summary>
    /// <param name="containerName">The name of the blob container.</param>
    /// <param name="blobName">The name of the blob.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The document content stream.</returns>
    Task<Stream> DownloadAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document from blob storage.
    /// </summary>
    /// <param name="containerName">The name of the blob container.</param>
    /// <param name="blobName">The name of the blob.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the blob was deleted, false if it didn't exist.</returns>
    Task<bool> DeleteAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a document exists in blob storage.
    /// </summary>
    /// <param name="containerName">The name of the blob container.</param>
    /// <param name="blobName">The name of the blob.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the blob exists, false otherwise.</returns>
    Task<bool> ExistsAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a shared access signature (SAS) URI for temporary access to a blob.
    /// </summary>
    /// <param name="containerName">The name of the blob container.</param>
    /// <param name="blobName">The name of the blob.</param>
    /// <param name="expiresIn">The duration for which the SAS token is valid.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The SAS URI for the blob.</returns>
    Task<string> GetSasUriAsync(
        string containerName,
        string blobName,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all blobs in a container with an optional prefix.
    /// </summary>
    /// <param name="containerName">The name of the blob container.</param>
    /// <param name="prefix">Optional prefix to filter blobs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of blob names.</returns>
    Task<IEnumerable<string>> ListBlobsAsync(
        string containerName,
        string? prefix = null,
        CancellationToken cancellationToken = default);
}
