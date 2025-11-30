using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using TaskMaster.DocumentService.Search.HealthChecks;
using TaskMaster.DocumentService.Search.Interfaces;

namespace TaskMaster.DocumentService.Search.Tests.HealthChecks;

/// <summary>
/// Unit tests for MeilisearchHealthCheck.
/// </summary>
public class MeilisearchHealthCheckTests
{
    [Fact]
    public void Constructor_WithNullSearchService_ShouldThrowArgumentNullException()
    {
        // Arrange
        ISearchService? nullService = null;

        // Act
        Action act = () => new MeilisearchHealthCheck(nullService!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("searchService");
    }

    [Fact]
    public void Constructor_WithValidSearchService_ShouldCreateInstance()
    {
        // Arrange
        var mockSearchService = new Mock<ISearchService>();

        // Act
        var healthCheck = new MeilisearchHealthCheck(mockSearchService.Object);

        // Assert
        healthCheck.Should().NotBeNull();
        healthCheck.Should().BeAssignableTo<IHealthCheck>();
    }

    [Fact]
    public async Task CheckHealthAsync_WhenSearchServiceIsHealthy_ShouldReturnHealthy()
    {
        // Arrange
        var mockSearchService = new Mock<ISearchService>();
        mockSearchService
            .Setup(s => s.IsHealthyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var healthCheck = new MeilisearchHealthCheck(mockSearchService.Object);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context, CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Be("Meilisearch is available and responding.");
        mockSearchService.Verify(s => s.IsHealthyAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenSearchServiceIsUnhealthy_ShouldReturnUnhealthy()
    {
        // Arrange
        var mockSearchService = new Mock<ISearchService>();
        mockSearchService
            .Setup(s => s.IsHealthyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var healthCheck = new MeilisearchHealthCheck(mockSearchService.Object);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context, CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("Meilisearch is not responding correctly.");
        mockSearchService.Verify(s => s.IsHealthyAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenSearchServiceThrowsException_ShouldReturnUnhealthy()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Test exception");
        var mockSearchService = new Mock<ISearchService>();
        mockSearchService
            .Setup(s => s.IsHealthyAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        var healthCheck = new MeilisearchHealthCheck(mockSearchService.Object);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context, CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("Meilisearch health check failed.");
        result.Exception.Should().BeSameAs(expectedException);
        mockSearchService.Verify(s => s.IsHealthyAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldPassCancellationToken()
    {
        // Arrange
        var mockSearchService = new Mock<ISearchService>();
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        mockSearchService
            .Setup(s => s.IsHealthyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var healthCheck = new MeilisearchHealthCheck(mockSearchService.Object);
        var context = new HealthCheckContext();

        // Act
        await healthCheck.CheckHealthAsync(context, cancellationToken);

        // Assert
        mockSearchService.Verify(
            s => s.IsHealthyAsync(cancellationToken),
            Times.Once);
    }
}
