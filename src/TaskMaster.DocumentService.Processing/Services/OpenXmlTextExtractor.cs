using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using TaskMaster.DocumentService.Processing.Interfaces;

namespace TaskMaster.DocumentService.Processing.Services;

/// <summary>
/// Extracts text content from Office Open XML documents (Word, Excel, PowerPoint).
/// </summary>
public class OpenXmlTextExtractor : ITextExtractor
{
    private static readonly string[] SupportedMimeTypes = new[]
    {
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document", // .docx
        "application/msword" // .doc (limited support)
    };

    /// <inheritdoc/>
    public async Task<string> ExtractTextAsync(Stream stream, string mimeType, CancellationToken cancellationToken = default)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        if (!SupportsType(mimeType))
            throw new NotSupportedException($"MIME type '{mimeType}' is not supported by OpenXmlTextExtractor.");

        return await Task.Run(() =>
        {
            var sb = new StringBuilder();

            using var wordDoc = WordprocessingDocument.Open(stream, false);
            var body = wordDoc.MainDocumentPart?.Document.Body;

            if (body != null)
            {
                foreach (var paragraph in body.Descendants<Paragraph>())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var text = paragraph.InnerText;
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        sb.AppendLine(text);
                    }
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
