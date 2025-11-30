using TaskMaster.DocumentService.Core.Models;

namespace TaskMaster.DocumentService.Processing.Interfaces;

/// <summary>
/// Interface for blob storage operations.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Lists all documents in the inbox container.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Collection of inbox documents.</returns>
    Task<IEnumerable<InboxDocument>> ListInboxDocumentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a document from the inbox.
    /// </summary>
    /// <param name="blobName">The blob name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The inbox document with content stream.</returns>
    Task<InboxDocument?> DownloadInboxDocumentAsync(string blobName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a document from inbox to processed container.
    /// </summary>
    /// <param name="blobName">The blob name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task MoveToProcessedAsync(string blobName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a document from inbox to failed container.
    /// </summary>
    /// <param name="blobName">The blob name.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task MoveToFailedAsync(string blobName, string errorMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a document to the main storage container.
    /// </summary>
    /// <param name="stream">The document stream.</param>
    /// <param name="blobPath">The blob path.</param>
    /// <param name="contentType">The content type.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task UploadDocumentAsync(Stream stream, string blobPath, string contentType, CancellationToken cancellationToken = default);
}
