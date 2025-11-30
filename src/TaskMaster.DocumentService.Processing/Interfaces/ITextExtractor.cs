namespace TaskMaster.DocumentService.Processing.Interfaces;

/// <summary>
/// Interface for extracting text content from documents.
/// </summary>
public interface ITextExtractor
{
    /// <summary>
    /// Extracts text content from a document stream.
    /// </summary>
    /// <param name="stream">The document stream.</param>
    /// <param name="mimeType">The MIME type of the document.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The extracted text content.</returns>
    Task<string> ExtractTextAsync(Stream stream, string mimeType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines if the extractor supports the specified MIME type.
    /// </summary>
    /// <param name="mimeType">The MIME type to check.</param>
    /// <returns>True if supported; otherwise, false.</returns>
    bool SupportsType(string mimeType);
}
