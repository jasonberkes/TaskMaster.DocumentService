using FluentAssertions;
using TaskMaster.DocumentService.Processing.Services;

namespace TaskMaster.DocumentService.Processing.Tests.Services;

/// <summary>
/// Unit tests for PdfTextExtractor service.
/// </summary>
public class PdfTextExtractorTests
{
    private readonly PdfTextExtractor _extractor;

    public PdfTextExtractorTests()
    {
        _extractor = new PdfTextExtractor();
    }

    [Fact]
    public void SupportsType_WithPdfMimeType_ShouldReturnTrue()
    {
        // Arrange
        var mimeType = "application/pdf";

        // Act
        var result = _extractor.SupportsType(mimeType);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("text/plain")]
    [InlineData("application/msword")]
    [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    public void SupportsType_WithNonPdfMimeType_ShouldReturnFalse(string mimeType)
    {
        // Act
        var result = _extractor.SupportsType(mimeType);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExtractTextAsync_WithUnsupportedMimeType_ShouldThrowNotSupportedException()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            _extractor.ExtractTextAsync(stream, "text/plain", CancellationToken.None));
    }

    [Fact]
    public async Task ExtractTextAsync_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _extractor.ExtractTextAsync(null!, "application/pdf", CancellationToken.None));
    }
}
