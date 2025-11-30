namespace TaskMaster.DocumentService.Core.Models;

/// <summary>
/// Represents the result of document processing operation.
/// </summary>
public class DocumentProcessingResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the processing was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the document identifier if processing succeeded.
    /// </summary>
    public long? DocumentId { get; set; }

    /// <summary>
    /// Gets or sets the extracted text content.
    /// </summary>
    public string? ExtractedText { get; set; }

    /// <summary>
    /// Gets or sets the content hash (SHA-256).
    /// </summary>
    public string? ContentHash { get; set; }

    /// <summary>
    /// Gets or sets the file size in bytes.
    /// </summary>
    public long? FileSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the detected MIME type.
    /// </summary>
    public string? MimeType { get; set; }

    /// <summary>
    /// Gets or sets the error message if processing failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the exception details if an error occurred.
    /// </summary>
    public string? ExceptionDetails { get; set; }

    /// <summary>
    /// Gets or sets the processing duration in milliseconds.
    /// </summary>
    public long ProcessingTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the blob name that was processed.
    /// </summary>
    public string? BlobName { get; set; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static DocumentProcessingResult CreateSuccess(long documentId, string extractedText, string contentHash, long fileSizeBytes, string mimeType, long processingTimeMs, string blobName)
    {
        return new DocumentProcessingResult
        {
            Success = true,
            DocumentId = documentId,
            ExtractedText = extractedText,
            ContentHash = contentHash,
            FileSizeBytes = fileSizeBytes,
            MimeType = mimeType,
            ProcessingTimeMs = processingTimeMs,
            BlobName = blobName
        };
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static DocumentProcessingResult CreateFailure(string errorMessage, string? exceptionDetails, long processingTimeMs, string blobName)
    {
        return new DocumentProcessingResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            ExceptionDetails = exceptionDetails,
            ProcessingTimeMs = processingTimeMs,
            BlobName = blobName
        };
    }
}
