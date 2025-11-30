using FluentAssertions;
using TaskMaster.DocumentService.Core.Models;

namespace TaskMaster.DocumentService.Core.Tests.Models;

/// <summary>
/// Unit tests for DocumentProcessingResult model.
/// </summary>
public class DocumentProcessingResultTests
{
    [Fact]
    public void CreateSuccess_ShouldReturnSuccessResult()
    {
        // Arrange
        var documentId = 123L;
        var extractedText = "Test content";
        var contentHash = "abc123hash";
        var fileSizeBytes = 1024L;
        var mimeType = "application/pdf";
        var processingTimeMs = 500L;
        var blobName = "test.pdf";

        // Act
        var result = DocumentProcessingResult.CreateSuccess(
            documentId,
            extractedText,
            contentHash,
            fileSizeBytes,
            mimeType,
            processingTimeMs,
            blobName);

        // Assert
        result.Success.Should().BeTrue();
        result.DocumentId.Should().Be(documentId);
        result.ExtractedText.Should().Be(extractedText);
        result.ContentHash.Should().Be(contentHash);
        result.FileSizeBytes.Should().Be(fileSizeBytes);
        result.MimeType.Should().Be(mimeType);
        result.ProcessingTimeMs.Should().Be(processingTimeMs);
        result.BlobName.Should().Be(blobName);
        result.ErrorMessage.Should().BeNull();
        result.ExceptionDetails.Should().BeNull();
    }

    [Fact]
    public void CreateFailure_ShouldReturnFailureResult()
    {
        // Arrange
        var errorMessage = "Processing failed";
        var exceptionDetails = "Stack trace here";
        var processingTimeMs = 250L;
        var blobName = "failed.pdf";

        // Act
        var result = DocumentProcessingResult.CreateFailure(
            errorMessage,
            exceptionDetails,
            processingTimeMs,
            blobName);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be(errorMessage);
        result.ExceptionDetails.Should().Be(exceptionDetails);
        result.ProcessingTimeMs.Should().Be(processingTimeMs);
        result.BlobName.Should().Be(blobName);
        result.DocumentId.Should().BeNull();
        result.ExtractedText.Should().BeNull();
        result.ContentHash.Should().BeNull();
    }

    [Fact]
    public void CreateFailure_WithNullExceptionDetails_ShouldWork()
    {
        // Arrange
        var errorMessage = "Simple error";
        var processingTimeMs = 100L;
        var blobName = "error.pdf";

        // Act
        var result = DocumentProcessingResult.CreateFailure(
            errorMessage,
            null,
            processingTimeMs,
            blobName);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be(errorMessage);
        result.ExceptionDetails.Should().BeNull();
    }
}
