using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Interfaces;
using TaskMaster.DocumentService.Core.Models;
using TaskMaster.DocumentService.Processing.Interfaces;

namespace TaskMaster.DocumentService.Processing.Services;

/// <summary>
/// Service for processing documents from the inbox.
/// </summary>
public class DocumentProcessor : IDocumentProcessor
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IEnumerable<ITextExtractor> _textExtractors;
    private readonly ILogger<DocumentProcessor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentProcessor"/> class.
    /// </summary>
    /// <param name="documentRepository">The document repository.</param>
    /// <param name="blobStorageService">The blob storage service.</param>
    /// <param name="textExtractors">The collection of text extractors.</param>
    /// <param name="logger">The logger.</param>
    public DocumentProcessor(
        IDocumentRepository documentRepository,
        IBlobStorageService blobStorageService,
        IEnumerable<ITextExtractor> textExtractors,
        ILogger<DocumentProcessor> logger)
    {
        _documentRepository = documentRepository ?? throw new ArgumentNullException(nameof(documentRepository));
        _blobStorageService = blobStorageService ?? throw new ArgumentNullException(nameof(blobStorageService));
        _textExtractors = textExtractors ?? throw new ArgumentNullException(nameof(textExtractors));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<DocumentProcessingResult> ProcessDocumentAsync(InboxDocument inboxDocument, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Processing document {BlobName}", inboxDocument.BlobName);

            if (inboxDocument.ContentStream == null)
            {
                throw new InvalidOperationException("Content stream is null");
            }

            // Determine MIME type
            var mimeType = inboxDocument.ContentType ?? "application/octet-stream";

            // Calculate content hash for deduplication
            string contentHash;
            using (var memoryStream = new MemoryStream())
            {
                await inboxDocument.ContentStream.CopyToAsync(memoryStream, cancellationToken);
                memoryStream.Position = 0;
                contentHash = ComputeSha256Hash(memoryStream);
                memoryStream.Position = 0;
                inboxDocument.ContentStream = memoryStream;
            }

            // Check for duplicate
            var existingDocument = await _documentRepository.GetByContentHashAsync(contentHash, inboxDocument.TenantId, cancellationToken);
            if (existingDocument != null)
            {
                _logger.LogWarning("Document {BlobName} is a duplicate of document {DocumentId}", inboxDocument.BlobName, existingDocument.Id);
                stopwatch.Stop();
                return DocumentProcessingResult.CreateSuccess(
                    existingDocument.Id,
                    existingDocument.ExtractedText ?? string.Empty,
                    contentHash,
                    inboxDocument.ContentLength,
                    mimeType,
                    stopwatch.ElapsedMilliseconds,
                    inboxDocument.BlobName);
            }

            // Extract text content
            string extractedText = string.Empty;
            var extractor = _textExtractors.FirstOrDefault(e => e.SupportsType(mimeType));
            if (extractor != null)
            {
                try
                {
                    if (inboxDocument.ContentStream is MemoryStream ms)
                    {
                        ms.Position = 0;
                    }
                    extractedText = await extractor.ExtractTextAsync(inboxDocument.ContentStream, mimeType, cancellationToken);
                    _logger.LogInformation("Extracted {Length} characters from {BlobName}", extractedText.Length, inboxDocument.BlobName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to extract text from {BlobName}, continuing without text extraction", inboxDocument.BlobName);
                }
            }
            else
            {
                _logger.LogWarning("No text extractor found for MIME type {MimeType}", mimeType);
            }

            // Upload to main storage
            var blobPath = $"{inboxDocument.TenantId}/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}/{inboxDocument.BlobName}";
            if (inboxDocument.ContentStream is MemoryStream ms2)
            {
                ms2.Position = 0;
            }
            await _blobStorageService.UploadDocumentAsync(inboxDocument.ContentStream, blobPath, mimeType, cancellationToken);

            // Create document entity
            var document = new Document
            {
                TenantId = inboxDocument.TenantId,
                DocumentTypeId = inboxDocument.DocumentTypeId,
                Title = Path.GetFileNameWithoutExtension(inboxDocument.BlobName),
                BlobPath = blobPath,
                ContentHash = contentHash,
                FileSizeBytes = inboxDocument.ContentLength,
                MimeType = mimeType,
                OriginalFileName = inboxDocument.BlobName,
                ExtractedText = extractedText,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "InboxProcessor"
            };

            // Save to database
            var savedDocument = await _documentRepository.AddAsync(document, cancellationToken);

            stopwatch.Stop();

            _logger.LogInformation(
                "Successfully processed document {BlobName} -> Document ID {DocumentId} in {ElapsedMs}ms",
                inboxDocument.BlobName,
                savedDocument.Id,
                stopwatch.ElapsedMilliseconds);

            return DocumentProcessingResult.CreateSuccess(
                savedDocument.Id,
                extractedText,
                contentHash,
                inboxDocument.ContentLength,
                mimeType,
                stopwatch.ElapsedMilliseconds,
                inboxDocument.BlobName);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error processing document {BlobName}", inboxDocument.BlobName);
            return DocumentProcessingResult.CreateFailure(
                ex.Message,
                ex.ToString(),
                stopwatch.ElapsedMilliseconds,
                inboxDocument.BlobName);
        }
    }

    private static string ComputeSha256Hash(Stream stream)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(stream);
        var sb = new StringBuilder();
        foreach (var b in hashBytes)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }
}
