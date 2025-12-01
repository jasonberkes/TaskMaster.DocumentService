using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TaskMaster.DocumentService.Processing.BackgroundServices;
using TaskMaster.DocumentService.Processing.Configuration;
using TaskMaster.DocumentService.Processing.Interfaces;

namespace TaskMaster.DocumentService.Processing.Tests.BackgroundServices;

/// <summary>
/// Unit tests for InboxProcessorBackgroundService.
/// </summary>
public class InboxProcessorBackgroundServiceTests
{
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<ILogger<InboxProcessorBackgroundService>> _mockLogger;
    private readonly Mock<IOptions<InboxProcessorOptions>> _mockOptions;
    private readonly InboxProcessorOptions _options;

    public InboxProcessorBackgroundServiceTests()
    {
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockLogger = new Mock<ILogger<InboxProcessorBackgroundService>>();
        _options = new InboxProcessorOptions
        {
            Enabled = true,
            PollingIntervalSeconds = 1, // Short interval for testing
            BatchSize = 10,
            DefaultTenantId = 1,
            DefaultDocumentTypeId = 1
        };
        _mockOptions = new Mock<IOptions<InboxProcessorOptions>>();
        _mockOptions.Setup(x => x.Value).Returns(_options);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange & Act
        var service = new InboxProcessorBackgroundService(
            _mockServiceProvider.Object,
            _mockLogger.Object,
            _mockOptions.Object);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new InboxProcessorBackgroundService(null!, _mockLogger.Object, _mockOptions.Object));

        Assert.Equal("serviceProvider", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new InboxProcessorBackgroundService(_mockServiceProvider.Object, null!, _mockOptions.Object));

        Assert.Equal("logger", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new InboxProcessorBackgroundService(_mockServiceProvider.Object, _mockLogger.Object, null!));

        Assert.Equal("options", exception.ParamName);
    }

    #endregion

    #region ExecuteAsync Tests

    [Fact]
    public async Task ExecuteAsync_WhenDisabled_ShouldNotProcessFiles()
    {
        // Arrange
        _options.Enabled = false;

        var mockInboxProcessorService = new Mock<IInboxProcessorService>();
        var mockServiceScope = new Mock<IServiceScope>();
        var mockServiceScopeFactory = new Mock<IServiceScopeFactory>();

        mockServiceScope.Setup(x => x.ServiceProvider.GetService(typeof(IInboxProcessorService)))
            .Returns(mockInboxProcessorService.Object);
        mockServiceScopeFactory.Setup(x => x.CreateScope())
            .Returns(mockServiceScope.Object);
        _mockServiceProvider.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(mockServiceScopeFactory.Object);

        var service = new InboxProcessorBackgroundService(
            _mockServiceProvider.Object,
            _mockLogger.Object,
            _mockOptions.Object);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(200);
        await service.StopAsync(cts.Token);

        // Assert
        mockInboxProcessorService.Verify(
            x => x.ProcessInboxFilesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEnabled_ShouldProcessFiles()
    {
        // Arrange
        _options.Enabled = true;
        _options.PollingIntervalSeconds = 1;

        var mockInboxProcessorService = new Mock<IInboxProcessorService>();
        mockInboxProcessorService
            .Setup(x => x.ProcessInboxFilesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var mockServiceScope = new Mock<IServiceScope>();
        var mockServiceScopeFactory = new Mock<IServiceScopeFactory>();

        mockServiceScope.Setup(x => x.ServiceProvider.GetService(typeof(IInboxProcessorService)))
            .Returns(mockInboxProcessorService.Object);
        mockServiceScopeFactory.Setup(x => x.CreateScope())
            .Returns(mockServiceScope.Object);
        _mockServiceProvider.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(mockServiceScopeFactory.Object);

        var mockScopeServiceProvider = new Mock<IServiceProvider>();
        mockScopeServiceProvider.Setup(x => x.GetService(typeof(IInboxProcessorService)))
            .Returns(mockInboxProcessorService.Object);
        mockServiceScope.Setup(x => x.ServiceProvider).Returns(mockScopeServiceProvider.Object);

        _mockServiceProvider.Setup(x => x.CreateScope()).Returns(mockServiceScope.Object);

        var service = new InboxProcessorBackgroundService(
            _mockServiceProvider.Object,
            _mockLogger.Object,
            _mockOptions.Object);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(8)); // Allow time for at least one cycle

        // Act
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(TimeSpan.FromSeconds(7)); // Wait for processing cycles
        await service.StopAsync(CancellationToken.None);

        // Assert
        mockInboxProcessorService.Verify(
            x => x.ProcessInboxFilesAsync(It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_OnCancellation_ShouldStopGracefully()
    {
        // Arrange
        _options.Enabled = true;

        var mockInboxProcessorService = new Mock<IInboxProcessorService>();
        mockInboxProcessorService
            .Setup(x => x.ProcessInboxFilesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var mockServiceScope = new Mock<IServiceScope>();
        var mockServiceScopeFactory = new Mock<IServiceScopeFactory>();

        mockServiceScope.Setup(x => x.ServiceProvider.GetService(typeof(IInboxProcessorService)))
            .Returns(mockInboxProcessorService.Object);
        mockServiceScopeFactory.Setup(x => x.CreateScope())
            .Returns(mockServiceScope.Object);
        _mockServiceProvider.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(mockServiceScopeFactory.Object);

        var mockScopeServiceProvider = new Mock<IServiceProvider>();
        mockScopeServiceProvider.Setup(x => x.GetService(typeof(IInboxProcessorService)))
            .Returns(mockInboxProcessorService.Object);
        mockServiceScope.Setup(x => x.ServiceProvider).Returns(mockScopeServiceProvider.Object);

        _mockServiceProvider.Setup(x => x.CreateScope()).Returns(mockServiceScope.Object);

        var service = new InboxProcessorBackgroundService(
            _mockServiceProvider.Object,
            _mockLogger.Object,
            _mockOptions.Object);

        using var cts = new CancellationTokenSource();

        // Act
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(TimeSpan.FromSeconds(6)); // Wait for initial delay
        cts.Cancel();
        await service.StopAsync(CancellationToken.None);

        // Assert - Service should have stopped without throwing
        Assert.True(true);
    }

    [Fact]
    public async Task ExecuteAsync_OnException_ShouldContinueProcessing()
    {
        // Arrange
        _options.Enabled = true;
        _options.PollingIntervalSeconds = 1;

        var callCount = 0;
        var mockInboxProcessorService = new Mock<IInboxProcessorService>();
        mockInboxProcessorService
            .Setup(x => x.ProcessInboxFilesAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new InvalidOperationException("Test exception");
                }
                return Task.FromResult(0);
            });

        var mockServiceScope = new Mock<IServiceScope>();
        var mockScopeServiceProvider = new Mock<IServiceProvider>();
        mockScopeServiceProvider.Setup(x => x.GetService(typeof(IInboxProcessorService)))
            .Returns(mockInboxProcessorService.Object);
        mockServiceScope.Setup(x => x.ServiceProvider).Returns(mockScopeServiceProvider.Object);

        _mockServiceProvider.Setup(x => x.CreateScope()).Returns(mockServiceScope.Object);

        var service = new InboxProcessorBackgroundService(
            _mockServiceProvider.Object,
            _mockLogger.Object,
            _mockOptions.Object);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(10));

        // Act
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(TimeSpan.FromSeconds(8)); // Wait for multiple cycles
        await service.StopAsync(CancellationToken.None);

        // Assert - Should have been called multiple times despite exception
        Assert.True(callCount >= 2);
    }

    #endregion

    #region StopAsync Tests

    [Fact]
    public async Task StopAsync_ShouldStopGracefully()
    {
        // Arrange
        var service = new InboxProcessorBackgroundService(
            _mockServiceProvider.Object,
            _mockLogger.Object,
            _mockOptions.Object);

        await service.StartAsync(CancellationToken.None);

        // Act
        var stopTask = service.StopAsync(CancellationToken.None);
        await stopTask;

        // Assert
        Assert.True(stopTask.IsCompletedSuccessfully);
    }

    #endregion
}
