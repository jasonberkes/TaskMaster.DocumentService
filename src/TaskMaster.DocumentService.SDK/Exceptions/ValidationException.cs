namespace TaskMaster.DocumentService.SDK.Exceptions;

/// <summary>
/// Exception thrown when validation fails.
/// </summary>
public class ValidationException : DocumentServiceException
{
    /// <summary>
    /// Gets the validation errors.
    /// </summary>
    public Dictionary<string, string[]> ValidationErrors { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class.
    /// </summary>
    /// <param name="validationErrors">The validation errors.</param>
    public ValidationException(Dictionary<string, string[]> validationErrors)
        : base("One or more validation errors occurred.", "VALIDATION_ERROR", 400)
    {
        ValidationErrors = validationErrors;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class with a custom message.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="validationErrors">The validation errors.</param>
    public ValidationException(string message, Dictionary<string, string[]> validationErrors)
        : base(message, "VALIDATION_ERROR", 400)
    {
        ValidationErrors = validationErrors;
    }
}
