using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TaskMaster.DocumentService.Api.Services;
using TaskMaster.DocumentService.Core.DTOs;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Data.Repositories;
using TaskMaster.DocumentService.Search.Services;

namespace TaskMaster.DocumentService.Core.Tests.Services;

/// <summary>
/// Unit tests for DocumentSearchService.
/// </summary>
public class DocumentSearchServiceTests
{
    private readonly Mock<IDocumentRepository> _repositoryMock;
    private readonly Mock<IMeilisearchService> _meilisearchServiceMock;
    private readonly Mock<ILogger<DocumentSearchService>> _loggerMock;
    private readonly DocumentSearchService _service;

    public DocumentSearchServiceTests()
    {
        _repositoryMock = new Mock<IDocumentRepository>();
        _meilisearchServiceMock = new Mock<IMeilisearchService>();
        _loggerMock = new Mock<ILogger<DocumentSearchService>>();
        _service = new DocumentSearchService(
            _repositoryMock.Object,
            _meilisearchServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task SearchAsync_WithValidQuery_ShouldReturnResults()
    {
        // Arrange
        var request = new DocumentSearchRequest
        {
            Query = "test document",
            TenantId = 1,
            Page = 1,
            PageSize = 20
        };

        var expectedResponse = new DocumentSearchResponse
        {
            Results = new List<DocumentSearchResult>
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
                    Score = 0.95
                }
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 20,
            TotalPages = 1,
            Query = "test document"
        };

        _meilisearchServiceMock
            .Setup(x => x.SearchDocumentsAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _service.SearchAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Results.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        result.Query.Should().Be("test document");
        _meilisearchServiceMock.Verify(
            x => x.SearchDocumentsAsync(request, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchAsync_WithEmptyQuery_ShouldReturnEmptyResults()
    {
        // Arrange
        var request = new DocumentSearchRequest
        {
            Query = "",
            Page = 1,
            PageSize = 20
        };

        var expectedResponse = new DocumentSearchResponse
        {
            Results = new List<DocumentSearchResult>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 20,
            TotalPages = 0,
            Query = ""
        };

        _meilisearchServiceMock
            .Setup(x => x.SearchDocumentsAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _service.SearchAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Results.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task IndexDocumentAsync_WithValidDocument_ShouldIndexSuccessfully()
    {
        // Arrange
        const long documentId = 1;
        var document = new Document
        {
            Id = documentId,
            TenantId = 1,
            DocumentTypeId = 1,
            Title = "Test Document",
            DocumentType = new DocumentType
            {
                Id = 1,
                Name = "Invoice",
                IsContentIndexed = true
            }
        };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _meilisearchServiceMock
            .Setup(x => x.IndexDocumentAsync(document, It.IsAny<CancellationToken>()))
            .ReturnsAsync("1_1");

        _repositoryMock
            .Setup(x => x.UpdateIndexingInfoAsync(documentId, "1_1", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.IndexDocumentAsync(documentId, CancellationToken.None);

        // Assert
        _repositoryMock.Verify(
            x => x.GetByIdAsync(documentId, It.IsAny<CancellationToken>()),
            Times.Once);
        _meilisearchServiceMock.Verify(
            x => x.IndexDocumentAsync(document, It.IsAny<CancellationToken>()),
            Times.Once);
        _repositoryMock.Verify(
            x => x.UpdateIndexingInfoAsync(documentId, "1_1", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task IndexDocumentAsync_WithNonExistentDocument_ShouldThrow()
    {
        // Arrange
        const long documentId = 999;

        _repositoryMock
            .Setup(x => x.GetByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);

        // Act
        var act = async () => await _service.IndexDocumentAsync(documentId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Document {documentId} not found");
    }

    [Fact]
    public async Task IndexDocumentAsync_WithNonIndexableDocumentType_ShouldNotIndex()
    {
        // Arrange
        const long documentId = 1;
        var document = new Document
        {
            Id = documentId,
            TenantId = 1,
            DocumentTypeId = 1,
            Title = "Test Document",
            DocumentType = new DocumentType
            {
                Id = 1,
                Name = "PrivateNote",
                IsContentIndexed = false
            }
        };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        // Act
        await _service.IndexDocumentAsync(documentId, CancellationToken.None);

        // Assert
        _meilisearchServiceMock.Verify(
            x => x.IndexDocumentAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ReindexTenantDocumentsAsync_WithDocuments_ShouldIndexAll()
    {
        // Arrange
        const int tenantId = 1;
        var documents = new List<Document>
        {
            new Document
            {
                Id = 1,
                TenantId = tenantId,
                DocumentTypeId = 1,
                Title = "Document 1",
                DocumentType = new DocumentType
                {
                    Id = 1,
                    Name = "Invoice",
                    IsContentIndexed = true
                }
            },
            new Document
            {
                Id = 2,
                TenantId = tenantId,
                DocumentTypeId = 1,
                Title = "Document 2",
                DocumentType = new DocumentType
                {
                    Id = 1,
                    Name = "Invoice",
                    IsContentIndexed = true
                }
            }
        };

        _repositoryMock
            .Setup(x => x.GetByTenantIdAsync(tenantId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        _meilisearchServiceMock
            .Setup(x => x.IndexDocumentsAsync(It.IsAny<IEnumerable<Document>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(x => x.UpdateIndexingInfoAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ReindexTenantDocumentsAsync(tenantId, CancellationToken.None);

        // Assert
        result.Should().Be(2);
        _meilisearchServiceMock.Verify(
            x => x.IndexDocumentsAsync(It.IsAny<IEnumerable<Document>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ReindexTenantDocumentsAsync_WithNoDocuments_ShouldReturnZero()
    {
        // Arrange
        const int tenantId = 1;
        var documents = new List<Document>();

        _repositoryMock
            .Setup(x => x.GetByTenantIdAsync(tenantId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        // Act
        var result = await _service.ReindexTenantDocumentsAsync(tenantId, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        _meilisearchServiceMock.Verify(
            x => x.IndexDocumentsAsync(It.IsAny<IEnumerable<Document>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RemoveFromIndexAsync_WithIndexedDocument_ShouldRemove()
    {
        // Arrange
        const long documentId = 1;
        var document = new Document
        {
            Id = documentId,
            TenantId = 1,
            MeilisearchId = "1_1"
        };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _meilisearchServiceMock
            .Setup(x => x.RemoveDocumentAsync("1_1", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.RemoveFromIndexAsync(documentId, CancellationToken.None);

        // Assert
        _meilisearchServiceMock.Verify(
            x => x.RemoveDocumentAsync("1_1", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RemoveFromIndexAsync_WithNonIndexedDocument_ShouldNotRemove()
    {
        // Arrange
        const long documentId = 1;
        var document = new Document
        {
            Id = documentId,
            TenantId = 1,
            MeilisearchId = null
        };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        // Act
        await _service.RemoveFromIndexAsync(documentId, CancellationToken.None);

        // Assert
        _meilisearchServiceMock.Verify(
            x => x.RemoveDocumentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RemoveFromIndexAsync_WithNonExistentDocument_ShouldNotThrow()
    {
        // Arrange
        const long documentId = 999;

        _repositoryMock
            .Setup(x => x.GetByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);

        // Act
        var act = async () => await _service.RemoveFromIndexAsync(documentId, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }
}
