using System.Text;
using TaskMaster.DocumentService.Processing.Interfaces;

namespace TaskMaster.DocumentService.Processing.Services;

/// <summary>
/// Extracts text content from plain text files.
/// </summary>
public class PlainTextExtractor : ITextExtractor
{
    private static readonly string[] SupportedMimeTypes = new[]
    {
        "text/plain",
        "text/csv",
        "text/html",
        "text/xml",
        "application/json",
        "application/xml"
    };

    /// <inheritdoc/>
    public async Task<string> ExtractTextAsync(Stream stream, string mimeType, CancellationToken cancellationToken = default)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        if (!SupportsType(mimeType))
            throw new NotSupportedException($"MIME type '{mimeType}' is not supported by PlainTextExtractor.");

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public bool SupportsType(string mimeType)
    {
        return SupportedMimeTypes.Contains(mimeType, StringComparer.OrdinalIgnoreCase);
    }
}
