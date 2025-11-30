using System.Net;
using System.Text;
using System.Text.Json;
using Moq;
using Moq.Protected;
using TaskMaster.DocumentService.SDK.Clients;
using TaskMaster.DocumentService.SDK.DTOs;

namespace TaskMaster.DocumentService.SDK.Tests.Clients;

/// <summary>
/// Unit tests for DocumentTypesClient.
/// </summary>
public class DocumentTypesClientTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly DocumentTypesClient _documentTypesClient;

    public DocumentTypesClientTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://api.example.com/")
        };
        _documentTypesClient = new DocumentTypesClient(_httpClient);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDocumentType_WhenExists()
    {
        // Arrange
        var documentTypeId = 1;
        var expectedDocumentType = new DocumentTypeDto
        {
            Id = documentTypeId,
            Name = "Invoice",
            DisplayName = "Invoice Document",
            IsActive = true
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedDocumentType);

        // Act
        var result = await _documentTypesClient.GetByIdAsync(documentTypeId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedDocumentType.Id, result.Id);
        Assert.Equal(expectedDocumentType.Name, result.Name);
    }

    [Fact]
    public async Task GetByNameAsync_ReturnsDocumentType_WhenExists()
    {
        // Arrange
        var typeName = "Invoice";
        var expectedDocumentType = new DocumentTypeDto
        {
            Id = 1,
            Name = typeName,
            DisplayName = "Invoice Document",
            IsActive = true
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedDocumentType);

        // Act
        var result = await _documentTypesClient.GetByNameAsync(typeName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedDocumentType.Name, result.Name);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsDocumentTypes_WhenExist()
    {
        // Arrange
        var expectedDocumentTypes = new List<DocumentTypeDto>
        {
            new() { Id = 1, Name = "Invoice", DisplayName = "Invoice", IsActive = true },
            new() { Id = 2, Name = "Contract", DisplayName = "Contract", IsActive = true }
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedDocumentTypes);

        // Act
        var result = await _documentTypesClient.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyActiveDocumentTypes_WhenActiveOnlyIsTrue()
    {
        // Arrange
        var expectedDocumentTypes = new List<DocumentTypeDto>
        {
            new() { Id = 1, Name = "Invoice", DisplayName = "Invoice", IsActive = true },
            new() { Id = 2, Name = "Contract", DisplayName = "Contract", IsActive = true }
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedDocumentTypes);

        // Act
        var result = await _documentTypesClient.GetAllAsync(activeOnly: true);

        // Assert
        Assert.NotNull(result);
        Assert.All(result, dt => Assert.True(dt.IsActive));
    }

    [Fact]
    public async Task CreateAsync_ReturnsCreatedDocumentType_WhenRequestIsValid()
    {
        // Arrange
        var documentType = new DocumentTypeDto
        {
            Name = "NewType",
            DisplayName = "New Document Type",
            IsActive = true
        };

        var expectedDocumentType = new DocumentTypeDto
        {
            Id = 1,
            Name = documentType.Name,
            DisplayName = documentType.DisplayName,
            IsActive = documentType.IsActive
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedDocumentType);

        // Act
        var result = await _documentTypesClient.CreateAsync(documentType);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedDocumentType.Name, result.Name);
        Assert.Equal(expectedDocumentType.DisplayName, result.DisplayName);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsUpdatedDocumentType_WhenRequestIsValid()
    {
        // Arrange
        var documentTypeId = 1;
        var documentType = new DocumentTypeDto
        {
            Id = documentTypeId,
            Name = "UpdatedType",
            DisplayName = "Updated Document Type",
            IsActive = true
        };

        SetupHttpResponse(HttpStatusCode.OK, documentType);

        // Act
        var result = await _documentTypesClient.UpdateAsync(documentTypeId, documentType);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(documentType.Name, result.Name);
        Assert.Equal(documentType.DisplayName, result.DisplayName);
    }

    [Fact]
    public async Task DeleteAsync_CompletesSuccessfully_WhenDocumentTypeExists()
    {
        // Arrange
        var documentTypeId = 1;
        SetupHttpResponse(HttpStatusCode.OK, new { });

        // Act
        await _documentTypesClient.DeleteAsync(documentTypeId);

        // Assert
        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
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
