using System.Net;
using System.Text;
using System.Text.Json;
using Moq;
using Moq.Protected;
using TaskMaster.DocumentService.SDK.Clients;
using TaskMaster.DocumentService.SDK.DTOs;
using TaskMaster.DocumentService.SDK.Exceptions;

namespace TaskMaster.DocumentService.SDK.Tests.Clients;

/// <summary>
/// Unit tests for DocumentsClient.
/// </summary>
public class DocumentsClientTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly DocumentsClient _documentsClient;

    public DocumentsClientTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://api.example.com/")
        };
        _documentsClient = new DocumentsClient(_httpClient);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDocument_WhenDocumentExists()
    {
        // Arrange
        var documentId = 123L;
        var expectedDocument = new DocumentDto
        {
            Id = documentId,
            Title = "Test Document",
            TenantId = 1,
            DocumentTypeId = 1
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedDocument);

        // Act
        var result = await _documentsClient.GetByIdAsync(documentId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedDocument.Id, result.Id);
        Assert.Equal(expectedDocument.Title, result.Title);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenDocumentNotFound()
    {
        // Arrange
        var documentId = 999L;
        SetupHttpResponse(HttpStatusCode.NotFound, new ApiResponse<object>
        {
            Success = false,
            ErrorMessage = "Document not found",
            ErrorCode = "DOCUMENT_NOT_FOUND"
        });

        // Act & Assert
        await Assert.ThrowsAsync<DocumentNotFoundException>(() =>
            _documentsClient.GetByIdAsync(documentId));
    }

    [Fact]
    public async Task GetByTenantIdAsync_ReturnsDocuments_WhenDocumentsExist()
    {
        // Arrange
        var tenantId = 1;
        var expectedDocuments = new List<DocumentDto>
        {
            new() { Id = 1, Title = "Doc 1", TenantId = tenantId, DocumentTypeId = 1 },
            new() { Id = 2, Title = "Doc 2", TenantId = tenantId, DocumentTypeId = 1 }
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedDocuments);

        // Act
        var result = await _documentsClient.GetByTenantIdAsync(tenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task CreateAsync_ReturnsCreatedDocument_WhenRequestIsValid()
    {
        // Arrange
        var request = new CreateDocumentRequest
        {
            TenantId = 1,
            DocumentTypeId = 1,
            Title = "New Document",
            BlobPath = "/docs/newdoc.pdf"
        };

        var expectedDocument = new DocumentDto
        {
            Id = 1,
            TenantId = request.TenantId,
            DocumentTypeId = request.DocumentTypeId,
            Title = request.Title,
            BlobPath = request.BlobPath
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedDocument);

        // Act
        var result = await _documentsClient.CreateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedDocument.Title, result.Title);
        Assert.Equal(expectedDocument.TenantId, result.TenantId);
    }

    [Fact]
    public async Task CreateAsync_ThrowsValidationException_WhenRequestIsInvalid()
    {
        // Arrange
        var request = new CreateDocumentRequest
        {
            TenantId = 1,
            DocumentTypeId = 1,
            Title = "", // Invalid: empty title
            BlobPath = "/docs/newdoc.pdf"
        };

        var validationErrors = new Dictionary<string, string[]>
        {
            ["Title"] = new[] { "Title is required" }
        };

        SetupHttpResponse(HttpStatusCode.BadRequest, new ApiResponse<object>
        {
            Success = false,
            ErrorMessage = "Validation failed",
            ErrorCode = "VALIDATION_ERROR",
            ValidationErrors = validationErrors
        });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            _documentsClient.CreateAsync(request));
        Assert.NotNull(exception.ValidationErrors);
        Assert.Contains("Title", exception.ValidationErrors.Keys);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsUpdatedDocument_WhenRequestIsValid()
    {
        // Arrange
        var documentId = 1L;
        var request = new UpdateDocumentRequest
        {
            Title = "Updated Title",
            Description = "Updated description"
        };

        var expectedDocument = new DocumentDto
        {
            Id = documentId,
            Title = request.Title,
            Description = request.Description,
            TenantId = 1,
            DocumentTypeId = 1
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedDocument);

        // Act
        var result = await _documentsClient.UpdateAsync(documentId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedDocument.Title, result.Title);
        Assert.Equal(expectedDocument.Description, result.Description);
    }

    [Fact]
    public async Task DeleteAsync_CompletesSuccessfully_WhenDocumentExists()
    {
        // Arrange
        var documentId = 1L;
        var deletedBy = "testuser";
        SetupHttpResponse(HttpStatusCode.OK, new { });

        // Act
        await _documentsClient.DeleteAsync(documentId, deletedBy);

        // Assert
        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task RestoreAsync_CompletesSuccessfully_WhenDocumentExists()
    {
        // Arrange
        var documentId = 1L;
        SetupHttpResponse(HttpStatusCode.OK, new { });

        // Act
        await _documentsClient.RestoreAsync(documentId);

        // Assert
        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ArchiveAsync_CompletesSuccessfully_WhenDocumentExists()
    {
        // Arrange
        var documentId = 1L;
        SetupHttpResponse(HttpStatusCode.OK, new { });

        // Act
        await _documentsClient.ArchiveAsync(documentId);

        // Assert
        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetVersionsAsync_ReturnsDocumentVersions_WhenVersionsExist()
    {
        // Arrange
        var parentDocumentId = 1L;
        var expectedVersions = new List<DocumentDto>
        {
            new() { Id = 1, Version = 1, ParentDocumentId = parentDocumentId },
            new() { Id = 2, Version = 2, ParentDocumentId = parentDocumentId },
            new() { Id = 3, Version = 3, ParentDocumentId = parentDocumentId }
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedVersions);

        // Act
        var result = await _documentsClient.GetVersionsAsync(parentDocumentId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task GetCurrentVersionAsync_ReturnsCurrentVersion_WhenExists()
    {
        // Arrange
        var parentDocumentId = 1L;
        var expectedVersion = new DocumentDto
        {
            Id = 3,
            Version = 3,
            ParentDocumentId = parentDocumentId,
            IsCurrentVersion = true
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedVersion);

        // Act
        var result = await _documentsClient.GetCurrentVersionAsync(parentDocumentId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Version);
        Assert.True(result.IsCurrentVersion);
    }

    [Fact]
    public async Task GetArchivedAsync_ReturnsArchivedDocuments_WhenExist()
    {
        // Arrange
        var tenantId = 1;
        var expectedDocuments = new List<DocumentDto>
        {
            new() { Id = 1, Title = "Archived Doc 1", IsArchived = true },
            new() { Id = 2, Title = "Archived Doc 2", IsArchived = true }
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedDocuments);

        // Act
        var result = await _documentsClient.GetArchivedAsync(tenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, doc => Assert.True(doc.IsArchived));
    }

    private void SetupHttpResponse<T>(HttpStatusCode statusCode, T content)
    {
        var json = JsonSerializer.Serialize(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var response = new HttpResponseMessage
        {
            StatusCode = statusCode,
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }
}
