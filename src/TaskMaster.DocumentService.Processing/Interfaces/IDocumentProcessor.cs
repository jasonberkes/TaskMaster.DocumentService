using TaskMaster.DocumentService.Core.Models;

namespace TaskMaster.DocumentService.Processing.Interfaces;

/// <summary>
/// Interface for processing documents from the inbox.
/// </summary>
public interface IDocumentProcessor
{
    /// <summary>
    /// Processes a document from the inbox.
    /// </summary>
    /// <param name="inboxDocument">The inbox document to process.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The processing result.</returns>
    Task<DocumentProcessingResult> ProcessDocumentAsync(InboxDocument inboxDocument, CancellationToken cancellationToken = default);
}
