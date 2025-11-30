using System.Text;
using FluentAssertions;
using TaskMaster.DocumentService.Processing.Services;

namespace TaskMaster.DocumentService.Processing.Tests.Services;

/// <summary>
/// Unit tests for PlainTextExtractor service.
/// </summary>
public class PlainTextExtractorTests
{
    private readonly PlainTextExtractor _extractor;

    public PlainTextExtractorTests()
    {
        _extractor = new PlainTextExtractor();
    }

    [Theory]
    [InlineData("text/plain")]
    [InlineData("text/csv")]
    [InlineData("text/html")]
    [InlineData("text/xml")]
    [InlineData("application/json")]
    [InlineData("application/xml")]
    public void SupportsType_WithSupportedMimeType_ShouldReturnTrue(string mimeType)
    {
        // Act
        var result = _extractor.SupportsType(mimeType);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("application/pdf")]
    [InlineData("application/msword")]
    [InlineData("image/png")]
    public void SupportsType_WithUnsupportedMimeType_ShouldReturnFalse(string mimeType)
    {
        // Act
        var result = _extractor.SupportsType(mimeType);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExtractTextAsync_WithPlainText_ShouldReturnContent()
    {
        // Arrange
        var content = "Hello, World!\nThis is a test document.";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var result = await _extractor.ExtractTextAsync(stream, "text/plain", CancellationToken.None);

        // Assert
        result.Should().Be(content);
    }

    [Fact]
    public async Task ExtractTextAsync_WithEmptyContent_ShouldReturnEmptyString()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act
        var result = await _extractor.ExtractTextAsync(stream, "text/plain", CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractTextAsync_WithUnsupportedMimeType_ShouldThrowNotSupportedException()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            _extractor.ExtractTextAsync(stream, "application/pdf", CancellationToken.None));
    }

    [Fact]
    public async Task ExtractTextAsync_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _extractor.ExtractTextAsync(null!, "text/plain", CancellationToken.None));
    }
}
