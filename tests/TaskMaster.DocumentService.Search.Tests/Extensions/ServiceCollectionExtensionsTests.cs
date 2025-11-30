using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskMaster.DocumentService.Search.Configuration;
using TaskMaster.DocumentService.Search.Extensions;
using TaskMaster.DocumentService.Search.Interfaces;

namespace TaskMaster.DocumentService.Search.Tests.Extensions;

/// <summary>
/// Unit tests for ServiceCollectionExtensions.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddDocumentServiceSearch_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection? nullServices = null;
        var configuration = new ConfigurationBuilder().Build();

        // Act
        Action act = () => nullServices!.AddDocumentServiceSearch(configuration);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddDocumentServiceSearch_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        IConfiguration? nullConfiguration = null;

        // Act
        Action act = () => services.AddDocumentServiceSearch(nullConfiguration!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("configuration");
    }

    [Fact]
    public void AddDocumentServiceSearch_WithValidConfiguration_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Add logging services
        var configurationData = new Dictionary<string, string>
        {
            { "Meilisearch:Url", "http://localhost:7700" },
            { "Meilisearch:ApiKey", "test-key" },
            { "Meilisearch:IndexName", "test-index" },
            { "Meilisearch:BatchSize", "50" },
            { "Meilisearch:SearchTimeoutSeconds", "10" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData!)
            .Build();

        // Act
        var result = services.AddDocumentServiceSearch(configuration);

        // Assert
        result.Should().BeSameAs(services);
        var serviceProvider = services.BuildServiceProvider();
        var searchService = serviceProvider.GetService<ISearchService>();
        searchService.Should().NotBeNull();
    }

    [Fact]
    public void AddDocumentServiceSearch_WithActionConfiguration_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection? nullServices = null;
        Action<MeilisearchOptions> configureOptions = options => { };

        // Act
        Action act = () => nullServices!.AddDocumentServiceSearch(configureOptions);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddDocumentServiceSearch_WithActionConfiguration_WithNullAction_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        Action<MeilisearchOptions>? nullAction = null;

        // Act
        Action act = () => services.AddDocumentServiceSearch(nullAction!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("configureOptions");
    }

    [Fact]
    public void AddDocumentServiceSearch_WithActionConfiguration_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        Action<MeilisearchOptions> configureOptions = options =>
        {
            options.Url = "http://custom:8700";
            options.ApiKey = "custom-key";
            options.IndexName = "custom-index";
            options.BatchSize = 25;
            options.SearchTimeoutSeconds = 15;
        };

        // Act
        var result = services.AddDocumentServiceSearch(configureOptions);

        // Assert
        result.Should().BeSameAs(services);
        var serviceProvider = services.BuildServiceProvider();
        var searchService = serviceProvider.GetService<ISearchService>();
        searchService.Should().NotBeNull();
    }

    [Fact]
    public void AddDocumentServiceSearch_ShouldRegisterAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "Meilisearch:Url", "http://localhost:7700" }
            }!)
            .Build();

        // Act
        services.AddDocumentServiceSearch(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var searchService1 = serviceProvider.GetService<ISearchService>();
        var searchService2 = serviceProvider.GetService<ISearchService>();

        searchService1.Should().NotBeNull();
        searchService2.Should().NotBeNull();
        searchService1.Should().BeSameAs(searchService2);
    }
}
