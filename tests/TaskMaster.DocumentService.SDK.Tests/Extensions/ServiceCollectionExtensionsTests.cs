using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskMaster.DocumentService.SDK.Configuration;
using TaskMaster.DocumentService.SDK.Extensions;
using TaskMaster.DocumentService.SDK.Interfaces;

namespace TaskMaster.DocumentService.SDK.Tests.Extensions;

/// <summary>
/// Unit tests for ServiceCollectionExtensions.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddDocumentServiceClient_RegistersServices_WhenCalledWithConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BaseUrl"] = "https://api.example.com",
                ["ApiKey"] = "test-key",
                ["TimeoutSeconds"] = "60"
            })
            .Build();

        // Act
        services.AddDocumentServiceClient(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetService<IDocumentServiceClient>();
        Assert.NotNull(client);
    }

    [Fact]
    public void AddDocumentServiceClient_RegistersServices_WhenCalledWithAction()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddDocumentServiceClient(options =>
        {
            options.BaseUrl = "https://api.example.com";
            options.ApiKey = "test-key";
            options.TimeoutSeconds = 60;
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetService<IDocumentServiceClient>();
        Assert.NotNull(client);
    }

    [Fact]
    public void AddDocumentServiceClient_RegistersServices_WhenCalledWithOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = new DocumentServiceOptions
        {
            BaseUrl = "https://api.example.com",
            ApiKey = "test-key",
            TimeoutSeconds = 60
        };

        // Act
        services.AddDocumentServiceClient(options);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetService<IDocumentServiceClient>();
        Assert.NotNull(client);
    }

    [Fact]
    public void AddDocumentServiceClient_RegistersIndividualClients_WhenCalled()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = new DocumentServiceOptions
        {
            BaseUrl = "https://api.example.com"
        };

        // Act
        services.AddDocumentServiceClient(options);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        Assert.NotNull(serviceProvider.GetService<IDocumentsClient>());
        Assert.NotNull(serviceProvider.GetService<IDocumentTypesClient>());
        Assert.NotNull(serviceProvider.GetService<ITenantsClient>());
    }

    [Fact]
    public void AddDocumentServiceClient_ThrowsArgumentException_WhenBaseUrlIsEmpty()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = new DocumentServiceOptions
        {
            BaseUrl = ""
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => services.AddDocumentServiceClient(options));
    }

    [Fact]
    public void AddDocumentServiceClient_ConfiguresHttpClient_WhenCalled()
    {
        // Arrange
        var services = new ServiceCollection();
        var baseUrl = "https://api.example.com";
        var options = new DocumentServiceOptions
        {
            BaseUrl = baseUrl,
            TimeoutSeconds = 120
        };

        // Act
        services.AddDocumentServiceClient(options);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetService<IDocumentServiceClient>();
        Assert.NotNull(client);
    }
}
