namespace TaskMaster.DocumentService.SDK.Exceptions;

/// <summary>
/// Exception thrown when a document is not found.
/// </summary>
public class DocumentNotFoundException : DocumentServiceException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentNotFoundException"/> class.
    /// </summary>
    /// <param name="documentId">The document identifier that was not found.</param>
    public DocumentNotFoundException(long documentId)
        : base($"Document with ID {documentId} was not found.", "DOCUMENT_NOT_FOUND", 404)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentNotFoundException"/> class with a custom message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public DocumentNotFoundException(string message)
        : base(message, "DOCUMENT_NOT_FOUND", 404)
    {
    }
}
