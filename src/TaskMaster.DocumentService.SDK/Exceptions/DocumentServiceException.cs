namespace TaskMaster.DocumentService.SDK.Exceptions;

/// <summary>
/// Base exception for Document Service SDK errors.
/// </summary>
public class DocumentServiceException : Exception
{
    /// <summary>
    /// Gets the error code associated with the exception.
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// Gets the HTTP status code if applicable.
    /// </summary>
    public int? StatusCode { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentServiceException"/> class.
    /// </summary>
    public DocumentServiceException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentServiceException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public DocumentServiceException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentServiceException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public DocumentServiceException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentServiceException"/> class with detailed error information.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="errorCode">The error code.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    public DocumentServiceException(string message, string? errorCode, int? statusCode) : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentServiceException"/> class with detailed error information and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="errorCode">The error code.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="innerException">The inner exception.</param>
    public DocumentServiceException(string message, string? errorCode, int? statusCode, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }
}
