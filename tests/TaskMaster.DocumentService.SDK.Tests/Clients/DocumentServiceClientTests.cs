using Moq;
using TaskMaster.DocumentService.SDK.Clients;
using TaskMaster.DocumentService.SDK.Interfaces;

namespace TaskMaster.DocumentService.SDK.Tests.Clients;

/// <summary>
/// Unit tests for DocumentServiceClient.
/// </summary>
public class DocumentServiceClientTests
{
    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenHttpClientIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DocumentServiceClient(null!));
    }

    [Fact]
    public void Documents_ReturnsDocumentsClient_WhenCalled()
    {
        // Arrange
        var httpClient = new HttpClient { BaseAddress = new Uri("https://api.example.com/") };
        var client = new DocumentServiceClient(httpClient);

        // Act
        var documentsClient = client.Documents;

        // Assert
        Assert.NotNull(documentsClient);
        Assert.IsAssignableFrom<IDocumentsClient>(documentsClient);
    }

    [Fact]
    public void DocumentTypes_ReturnsDocumentTypesClient_WhenCalled()
    {
        // Arrange
        var httpClient = new HttpClient { BaseAddress = new Uri("https://api.example.com/") };
        var client = new DocumentServiceClient(httpClient);

        // Act
        var documentTypesClient = client.DocumentTypes;

        // Assert
        Assert.NotNull(documentTypesClient);
        Assert.IsAssignableFrom<IDocumentTypesClient>(documentTypesClient);
    }

    [Fact]
    public void Tenants_ReturnsTenantsClient_WhenCalled()
    {
        // Arrange
        var httpClient = new HttpClient { BaseAddress = new Uri("https://api.example.com/") };
        var client = new DocumentServiceClient(httpClient);

        // Act
        var tenantsClient = client.Tenants;

        // Assert
        Assert.NotNull(tenantsClient);
        Assert.IsAssignableFrom<ITenantsClient>(tenantsClient);
    }

    [Fact]
    public void Documents_ReturnsSameInstance_WhenCalledMultipleTimes()
    {
        // Arrange
        var httpClient = new HttpClient { BaseAddress = new Uri("https://api.example.com/") };
        var client = new DocumentServiceClient(httpClient);

        // Act
        var documentsClient1 = client.Documents;
        var documentsClient2 = client.Documents;

        // Assert
        Assert.Same(documentsClient1, documentsClient2);
    }

    [Fact]
    public void Dispose_DisposesHttpClient_WhenCalled()
    {
        // Arrange
        var httpClient = new HttpClient { BaseAddress = new Uri("https://api.example.com/") };
        var client = new DocumentServiceClient(httpClient);

        // Act
        client.Dispose();

        // Assert - calling a method on disposed HttpClient should throw
        Assert.Throws<ObjectDisposedException>(() => httpClient.GetAsync("/test").Wait());
    }
}
