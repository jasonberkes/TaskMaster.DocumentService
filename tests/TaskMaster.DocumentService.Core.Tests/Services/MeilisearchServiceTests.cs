using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TaskMaster.DocumentService.Core.DTOs;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Search.Configuration;
using TaskMaster.DocumentService.Search.Services;

namespace TaskMaster.DocumentService.Core.Tests.Services;

/// <summary>
/// Unit tests for MeilisearchService.
/// </summary>
public class MeilisearchServiceTests
{
    private readonly Mock<ILogger<MeilisearchService>> _loggerMock;
    private readonly MeilisearchSettings _settings;

    public MeilisearchServiceTests()
    {
        _loggerMock = new Mock<ILogger<MeilisearchService>>();
        _settings = new MeilisearchSettings
        {
            Url = "http://localhost:7700",
            IndexName = "test-documents",
            ApiKey = "test-key",
            TimeoutSeconds = 30,
            BatchSize = 100
        };
    }

    [Fact]
    public void Constructor_WithValidSettings_ShouldCreateInstance()
    {
        // Arrange
        var options = Options.Create(_settings);

        // Act
        var service = new MeilisearchService(options, _loggerMock.Object);

        // Assert
        service.Should().NotBeNull();
    }


    [Fact]
    public async Task HealthCheckAsync_WhenServiceIsDown_ShouldReturnFalse()
    {
        // Arrange
        var options = Options.Create(_settings);
        var service = new MeilisearchService(options, _loggerMock.Object);

        // Act
        var result = await service.HealthCheckAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void MeilisearchSettings_DefaultValues_ShouldBeCorrect()
    {
        // Arrange
        var settings = new MeilisearchSettings();

        // Assert
        settings.Url.Should().Be("http://localhost:7700");
        settings.IndexName.Should().Be("documents");
        settings.TimeoutSeconds.Should().Be(30);
        settings.BatchSize.Should().Be(100);
    }

    [Theory]
    [InlineData("http://localhost:7700", "documents", "test-key", 30, 100)]
    [InlineData("http://meilisearch:7700", "prod-docs", "", 60, 200)]
    public void MeilisearchSettings_CustomValues_ShouldBeSet(
        string url,
        string indexName,
        string apiKey,
        int timeout,
        int batchSize)
    {
        // Arrange & Act
        var settings = new MeilisearchSettings
        {
            Url = url,
            IndexName = indexName,
            ApiKey = apiKey,
            TimeoutSeconds = timeout,
            BatchSize = batchSize
        };

        // Assert
        settings.Url.Should().Be(url);
        settings.IndexName.Should().Be(indexName);
        settings.ApiKey.Should().Be(apiKey);
        settings.TimeoutSeconds.Should().Be(timeout);
        settings.BatchSize.Should().Be(batchSize);
    }

    [Fact]
    public void DocumentSearchRequest_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var request = new DocumentSearchRequest();

        // Assert
        request.Query.Should().Be(string.Empty);
        request.Page.Should().Be(1);
        request.PageSize.Should().Be(20);
        request.IncludeArchived.Should().BeFalse();
        request.SortDescending.Should().BeFalse();
    }

    [Fact]
    public void DocumentSearchRequest_WithValues_ShouldSetProperties()
    {
        // Arrange & Act
        var request = new DocumentSearchRequest
        {
            Query = "test search",
            TenantId = 1,
            DocumentTypeId = 2,
            Tags = new List<string> { "tag1", "tag2" },
            Page = 2,
            PageSize = 50,
            IncludeArchived = true,
            SortBy = "createdAt",
            SortDescending = true
        };

        // Assert
        request.Query.Should().Be("test search");
        request.TenantId.Should().Be(1);
        request.DocumentTypeId.Should().Be(2);
        request.Tags.Should().HaveCount(2);
        request.Page.Should().Be(2);
        request.PageSize.Should().Be(50);
        request.IncludeArchived.Should().BeTrue();
        request.SortBy.Should().Be("createdAt");
        request.SortDescending.Should().BeTrue();
    }

    [Fact]
    public void DocumentSearchResponse_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var response = new DocumentSearchResponse();

        // Assert
        response.Results.Should().BeEmpty();
        response.TotalCount.Should().Be(0);
        response.Page.Should().Be(0);
        response.PageSize.Should().Be(0);
        response.TotalPages.Should().Be(0);
        response.ProcessingTimeMs.Should().Be(0);
        response.Query.Should().Be(string.Empty);
    }

    [Fact]
    public void DocumentSearchResponse_WithResults_ShouldSetProperties()
    {
        // Arrange
        var results = new List<DocumentSearchResult>
        {
            new DocumentSearchResult
            {
                Document = new DocumentDto
                {
                    Id = 1,
                    Title = "Test Document",
                    TenantId = 1,
                    DocumentTypeId = 1
                },
                Score = 0.95,
                Highlights = new List<string> { "test highlight" }
            }
        };

        // Act
        var response = new DocumentSearchResponse
        {
            Results = results,
            TotalCount = 100,
            Page = 1,
            PageSize = 20,
            TotalPages = 5,
            ProcessingTimeMs = 150,
            Query = "test"
        };

        // Assert
        response.Results.Should().HaveCount(1);
        response.TotalCount.Should().Be(100);
        response.Page.Should().Be(1);
        response.PageSize.Should().Be(20);
        response.TotalPages.Should().Be(5);
        response.ProcessingTimeMs.Should().Be(150);
        response.Query.Should().Be("test");
    }

    [Fact]
    public void DocumentDto_Properties_ShouldSetCorrectly()
    {
        // Arrange & Act
        var dto = new DocumentDto
        {
            Id = 123,
            TenantId = 1,
            DocumentTypeId = 2,
            DocumentTypeName = "Invoice",
            Title = "Test Invoice",
            Description = "Test Description",
            OriginalFileName = "invoice.pdf",
            MimeType = "application/pdf",
            FileSizeBytes = 1024,
            Tags = new List<string> { "finance", "2024" },
            Version = 1,
            IsCurrentVersion = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "user@test.com",
            UpdatedAt = DateTime.UtcNow,
            IsArchived = false
        };

        // Assert
        dto.Id.Should().Be(123);
        dto.TenantId.Should().Be(1);
        dto.DocumentTypeId.Should().Be(2);
        dto.DocumentTypeName.Should().Be("Invoice");
        dto.Title.Should().Be("Test Invoice");
        dto.Description.Should().Be("Test Description");
        dto.OriginalFileName.Should().Be("invoice.pdf");
        dto.MimeType.Should().Be("application/pdf");
        dto.FileSizeBytes.Should().Be(1024);
        dto.Tags.Should().HaveCount(2);
        dto.Version.Should().Be(1);
        dto.IsCurrentVersion.Should().BeTrue();
        dto.CreatedBy.Should().Be("user@test.com");
        dto.IsArchived.Should().BeFalse();
    }
}
