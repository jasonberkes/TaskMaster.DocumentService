using System.Text;
using TaskMaster.DocumentService.Processing.Interfaces;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace TaskMaster.DocumentService.Processing.Services;

/// <summary>
/// Extracts text content from PDF documents using PdfPig.
/// </summary>
public class PdfTextExtractor : ITextExtractor
{
    private static readonly string[] SupportedMimeTypes = new[]
    {
        "application/pdf"
    };

    /// <inheritdoc/>
    public async Task<string> ExtractTextAsync(Stream stream, string mimeType, CancellationToken cancellationToken = default)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        if (!SupportsType(mimeType))
            throw new NotSupportedException($"MIME type '{mimeType}' is not supported by PdfTextExtractor.");

        return await Task.Run(() =>
        {
            var sb = new StringBuilder();

            using var document = PdfDocument.Open(stream);
            foreach (Page page in document.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var text = page.Text;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    sb.AppendLine(text);
                }
            }

            return sb.ToString();
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public bool SupportsType(string mimeType)
    {
        return SupportedMimeTypes.Contains(mimeType, StringComparer.OrdinalIgnoreCase);
    }
}
