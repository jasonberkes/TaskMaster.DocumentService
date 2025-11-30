using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Search.Configuration;
using TaskMaster.DocumentService.Search.Interfaces;
using TaskMaster.DocumentService.Search.Models;
using TaskMaster.DocumentService.Search.Services;

namespace TaskMaster.DocumentService.Search.Tests.Services;

/// <summary>
/// Unit tests for MeilisearchService.
/// </summary>
public class MeilisearchServiceTests
{
    private readonly Mock<ILogger<MeilisearchService>> _mockLogger;
    private readonly MeilisearchOptions _options;

    public MeilisearchServiceTests()
    {
        _mockLogger = new Mock<ILogger<MeilisearchService>>();
        _options = new MeilisearchOptions
        {
            Url = "http://localhost:7700",
            ApiKey = "test-key",
            IndexName = "test-documents",
            BatchSize = 10,
            SearchTimeoutSeconds = 5
        };
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange
        IOptions<MeilisearchOptions>? nullOptions = null;

        // Act
        Action act = () => new MeilisearchService(nullOptions!, _mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(_options);
        ILogger<MeilisearchService>? nullLogger = null;

        // Act
        Action act = () => new MeilisearchService(options, nullLogger!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange
        var options = Options.Create(_options);

        // Act
        var service = new MeilisearchService(options, _mockLogger.Object);

        // Assert
        service.Should().NotBeNull();
        service.Should().BeAssignableTo<ISearchService>();
    }

    [Fact]
    public async Task IndexDocumentAsync_WithNullDocument_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(_options);
        var service = new MeilisearchService(options, _mockLogger.Object);
        Document? nullDocument = null;

        // Act
        Func<Task> act = async () => await service.IndexDocumentAsync(nullDocument!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("document");
    }

    [Fact]
    public async Task IndexDocumentsBatchAsync_WithNullDocuments_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(_options);
        var service = new MeilisearchService(options, _mockLogger.Object);
        IEnumerable<Document>? nullDocuments = null;

        // Act
        Func<Task> act = async () => await service.IndexDocumentsBatchAsync(nullDocuments!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("documents");
    }

    [Fact]
    public async Task IndexDocumentsBatchAsync_WithEmptyDocuments_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var options = Options.Create(_options);
        var service = new MeilisearchService(options, _mockLogger.Object);
        var emptyDocuments = Enumerable.Empty<Document>();

        // Act
        var result = await service.IndexDocumentsBatchAsync(emptyDocuments, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateDocumentAsync_WithNullDocument_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(_options);
        var service = new MeilisearchService(options, _mockLogger.Object);
        Document? nullDocument = null;

        // Act
        Func<Task> act = async () => await service.UpdateDocumentAsync(nullDocument!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("document");
    }

    [Fact]
    public async Task RemoveDocumentAsync_WithNullOrEmptyId_ShouldThrowArgumentException()
    {
        // Arrange
        var options = Options.Create(_options);
        var service = new MeilisearchService(options, _mockLogger.Object);

        // Act
        Func<Task> actNull = async () => await service.RemoveDocumentAsync(null!, CancellationToken.None);
        Func<Task> actEmpty = async () => await service.RemoveDocumentAsync(string.Empty, CancellationToken.None);

        // Assert
        await actNull.Should().ThrowAsync<ArgumentException>().WithParameterName("meilisearchId");
        await actEmpty.Should().ThrowAsync<ArgumentException>().WithParameterName("meilisearchId");
    }

    [Fact]
    public async Task RemoveDocumentsBatchAsync_WithNullIds_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(_options);
        var service = new MeilisearchService(options, _mockLogger.Object);
        IEnumerable<string>? nullIds = null;

        // Act
        Func<Task> act = async () => await service.RemoveDocumentsBatchAsync(nullIds!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("meilisearchIds");
    }

    [Fact]
    public async Task RemoveDocumentsBatchAsync_WithEmptyIds_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_options);
        var service = new MeilisearchService(options, _mockLogger.Object);
        var emptyIds = Enumerable.Empty<string>();

        // Act
        Func<Task> act = async () => await service.RemoveDocumentsBatchAsync(emptyIds, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SearchDocumentsAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(_options);
        var service = new MeilisearchService(options, _mockLogger.Object);
        SearchRequest? nullRequest = null;

        // Act
        Func<Task> act = async () => await service.SearchDocumentsAsync(nullRequest!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("searchRequest");
    }

    [Fact]
    public void MeilisearchOptions_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new MeilisearchOptions();

        // Assert
        options.Url.Should().Be("http://localhost:7700");
        options.IndexName.Should().Be("documents");
        options.BatchSize.Should().Be(100);
        options.SearchTimeoutSeconds.Should().Be(5);
        MeilisearchOptions.SectionName.Should().Be("Meilisearch");
    }

    [Fact]
    public void MeilisearchOptions_CanSetCustomValues()
    {
        // Arrange & Act
        var options = new MeilisearchOptions
        {
            Url = "http://custom:8700",
            ApiKey = "custom-key",
            IndexName = "custom-index",
            BatchSize = 50,
            SearchTimeoutSeconds = 10
        };

        // Assert
        options.Url.Should().Be("http://custom:8700");
        options.ApiKey.Should().Be("custom-key");
        options.IndexName.Should().Be("custom-index");
        options.BatchSize.Should().Be(50);
        options.SearchTimeoutSeconds.Should().Be(10);
    }

    [Fact]
    public void SearchRequest_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var request = new SearchRequest();

        // Assert
        request.Query.Should().Be(string.Empty);
        request.OnlyCurrentVersion.Should().BeTrue();
        request.Page.Should().Be(1);
        request.PageSize.Should().Be(20);
        request.TenantId.Should().BeNull();
        request.DocumentTypeId.Should().BeNull();
        request.Tags.Should().BeNull();
        request.MimeType.Should().BeNull();
    }

    [Fact]
    public void SearchRequest_CanSetCustomValues()
    {
        // Arrange & Act
        var request = new SearchRequest
        {
            Query = "test query",
            TenantId = 123,
            DocumentTypeId = 456,
            Tags = "tag1,tag2",
            MimeType = "application/pdf",
            OnlyCurrentVersion = false,
            CreatedFrom = new DateTime(2024, 1, 1),
            CreatedTo = new DateTime(2024, 12, 31),
            Page = 2,
            PageSize = 50
        };

        // Assert
        request.Query.Should().Be("test query");
        request.TenantId.Should().Be(123);
        request.DocumentTypeId.Should().Be(456);
        request.Tags.Should().Be("tag1,tag2");
        request.MimeType.Should().Be("application/pdf");
        request.OnlyCurrentVersion.Should().BeFalse();
        request.CreatedFrom.Should().Be(new DateTime(2024, 1, 1));
        request.CreatedTo.Should().Be(new DateTime(2024, 12, 31));
        request.Page.Should().Be(2);
        request.PageSize.Should().Be(50);
    }

    [Fact]
    public void SearchableDocument_ShouldMapAllProperties()
    {
        // Arrange & Act
        var doc = new SearchableDocument
        {
            Id = "doc_123",
            DocumentId = 123,
            TenantId = 1,
            DocumentTypeId = 2,
            Title = "Test Document",
            Description = "Test Description",
            ExtractedText = "Extracted text content",
            OriginalFileName = "test.pdf",
            MimeType = "application/pdf",
            Tags = "tag1,tag2",
            Metadata = "{\"key\":\"value\"}",
            FileSizeBytes = 12345,
            Version = 1,
            IsCurrentVersion = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "user1",
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = "user2"
        };

        // Assert
        doc.Id.Should().Be("doc_123");
        doc.DocumentId.Should().Be(123);
        doc.TenantId.Should().Be(1);
        doc.DocumentTypeId.Should().Be(2);
        doc.Title.Should().Be("Test Document");
        doc.Description.Should().Be("Test Description");
        doc.ExtractedText.Should().Be("Extracted text content");
        doc.OriginalFileName.Should().Be("test.pdf");
        doc.MimeType.Should().Be("application/pdf");
        doc.Tags.Should().Be("tag1,tag2");
        doc.Metadata.Should().Be("{\"key\":\"value\"}");
        doc.FileSizeBytes.Should().Be(12345);
        doc.Version.Should().Be(1);
        doc.IsCurrentVersion.Should().BeTrue();
        doc.CreatedBy.Should().Be("user1");
        doc.UpdatedBy.Should().Be("user2");
    }

    [Fact]
    public void SearchResult_ShouldMapAllProperties()
    {
        // Arrange
        var hits = new List<SearchableDocument>
        {
            new SearchableDocument { Id = "1", DocumentId = 1, Title = "Doc 1" },
            new SearchableDocument { Id = "2", DocumentId = 2, Title = "Doc 2" }
        };

        // Act
        var result = new SearchResult<SearchableDocument>
        {
            Hits = hits,
            TotalHits = 2,
            PageSize = 20,
            Page = 1,
            TotalPages = 1,
            ProcessingTimeMs = 50,
            Query = "test"
        };

        // Assert
        result.Hits.Should().HaveCount(2);
        result.TotalHits.Should().Be(2);
        result.PageSize.Should().Be(20);
        result.Page.Should().Be(1);
        result.TotalPages.Should().Be(1);
        result.ProcessingTimeMs.Should().Be(50);
        result.Query.Should().Be("test");
    }

    private Document CreateTestDocument(long id = 1, int tenantId = 1, int documentTypeId = 1)
    {
        return new Document
        {
            Id = id,
            TenantId = tenantId,
            DocumentTypeId = documentTypeId,
            Title = $"Test Document {id}",
            Description = "Test Description",
            BlobPath = $"/blobs/test-{id}.pdf",
            ContentHash = "abc123",
            FileSizeBytes = 12345,
            MimeType = "application/pdf",
            OriginalFileName = $"test-{id}.pdf",
            Metadata = "{\"key\":\"value\"}",
            Tags = "tag1,tag2",
            ExtractedText = "This is extracted text content for searching",
            Version = 1,
            IsCurrentVersion = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user",
            IsDeleted = false,
            IsArchived = false,
            Tenant = new Tenant
            {
                Id = tenantId,
                TenantType = "Organization",
                Name = "Test Org",
                Slug = "test-org",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            DocumentType = new DocumentType
            {
                Id = documentTypeId,
                Name = "Invoice",
                DisplayName = "Invoice",
                IsContentIndexed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };
    }
}
