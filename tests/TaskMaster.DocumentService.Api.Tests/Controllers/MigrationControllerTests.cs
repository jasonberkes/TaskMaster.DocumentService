using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TaskMaster.DocumentService.Api.Controllers;
using TaskMaster.DocumentService.Core.DTOs;
using TaskMaster.DocumentService.Core.Interfaces;
using Xunit;

namespace TaskMaster.DocumentService.Api.Tests.Controllers;

/// <summary>
/// Unit tests for MigrationController.
/// WI #2298: Document Service: Migrate Existing Code Reviews from TaskMaster.Platform
/// </summary>
public class MigrationControllerTests
{
    private readonly Mock<ICodeReviewMigrationService> _mockMigrationService;
    private readonly Mock<ILogger<MigrationController>> _mockLogger;
    private readonly MigrationController _controller;

    public MigrationControllerTests()
    {
        _mockMigrationService = new Mock<ICodeReviewMigrationService>();
        _mockLogger = new Mock<ILogger<MigrationController>>();
        _controller = new MigrationController(_mockMigrationService.Object, _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange & Act
        var controller = new MigrationController(_mockMigrationService.Object, _mockLogger.Object);

        // Assert
        Assert.NotNull(controller);
    }

    [Fact]
    public void Constructor_WithNullMigrationService_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new MigrationController(null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new MigrationController(_mockMigrationService.Object, null!));
    }

    #endregion

    #region MigrateCodeReviews Tests

    [Fact]
    public async Task MigrateCodeReviews_WithNullRequest_ReturnsBadRequest()
    {
        // Arrange & Act
        var result = await _controller.MigrateCodeReviews(null!, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Migration request cannot be null.", badRequestResult.Value);
    }

    [Fact]
    public async Task MigrateCodeReviews_WithNullCodeReviews_ReturnsBadRequest()
    {
        // Arrange
        var request = new MigrateCodeReviewsRequest
        {
            TenantId = 1,
            CodeReviews = null
        };

        // Act
        var result = await _controller.MigrateCodeReviews(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("No code reviews provided for migration.", badRequestResult.Value);
    }

    [Fact]
    public async Task MigrateCodeReviews_WithEmptyCodeReviews_ReturnsBadRequest()
    {
        // Arrange
        var request = new MigrateCodeReviewsRequest
        {
            TenantId = 1,
            CodeReviews = new List<CodeReviewMigrationDto>()
        };

        // Act
        var result = await _controller.MigrateCodeReviews(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("No code reviews provided for migration.", badRequestResult.Value);
    }

    [Fact]
    public async Task MigrateCodeReviews_WithInvalidTenantId_ReturnsBadRequest()
    {
        // Arrange
        var request = new MigrateCodeReviewsRequest
        {
            TenantId = 0,
            CodeReviews = new List<CodeReviewMigrationDto>
            {
                new CodeReviewMigrationDto { SourceCodeReviewId = 1 }
            }
        };

        // Act
        var result = await _controller.MigrateCodeReviews(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Invalid tenant ID.", badRequestResult.Value);
    }

    [Fact]
    public async Task MigrateCodeReviews_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var request = new MigrateCodeReviewsRequest
        {
            TenantId = 1,
            CodeReviews = new List<CodeReviewMigrationDto>
            {
                new CodeReviewMigrationDto { SourceCodeReviewId = 1 }
            }
        };

        var expectedResponse = new MigrateCodeReviewsResponse
        {
            MigratedCount = 1,
            SkippedCount = 0,
            FailedCount = 0,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };

        _mockMigrationService
            .Setup(x => x.MigrateCodeReviewsAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.MigrateCodeReviews(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<MigrateCodeReviewsResponse>(okResult.Value);
        Assert.Equal(1, response.MigratedCount);
        Assert.Equal(0, response.FailedCount);
    }

    [Fact]
    public async Task MigrateCodeReviews_WithSuccessfulMigration_LogsInformation()
    {
        // Arrange
        var request = new MigrateCodeReviewsRequest
        {
            TenantId = 1,
            CodeReviews = new List<CodeReviewMigrationDto>
            {
                new CodeReviewMigrationDto { SourceCodeReviewId = 1 }
            }
        };

        var expectedResponse = new MigrateCodeReviewsResponse
        {
            MigratedCount = 5,
            SkippedCount = 2,
            FailedCount = 0,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };

        _mockMigrationService
            .Setup(x => x.MigrateCodeReviewsAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        await _controller.MigrateCodeReviews(request, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("completed successfully")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task MigrateCodeReviews_WithPartialFailures_LogsWarning()
    {
        // Arrange
        var request = new MigrateCodeReviewsRequest
        {
            TenantId = 1,
            CodeReviews = new List<CodeReviewMigrationDto>
            {
                new CodeReviewMigrationDto { SourceCodeReviewId = 1 }
            }
        };

        var expectedResponse = new MigrateCodeReviewsResponse
        {
            MigratedCount = 3,
            SkippedCount = 0,
            FailedCount = 2,
            Errors = new List<string> { "Error 1", "Error 2" },
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };

        _mockMigrationService
            .Setup(x => x.MigrateCodeReviewsAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        await _controller.MigrateCodeReviews(request, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("completed with errors")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task MigrateCodeReviews_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var request = new MigrateCodeReviewsRequest
        {
            TenantId = 1,
            CodeReviews = new List<CodeReviewMigrationDto>
            {
                new CodeReviewMigrationDto { SourceCodeReviewId = 1 }
            }
        };

        _mockMigrationService
            .Setup(x => x.MigrateCodeReviewsAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _controller.MigrateCodeReviews(request, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
    }

    #endregion

    #region ValidateTenant Tests

    [Fact]
    public async Task ValidateTenant_WithInvalidTenantId_ReturnsBadRequest()
    {
        // Arrange
        var tenantId = 0;

        // Act
        var result = await _controller.ValidateTenant(tenantId, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var value = badRequestResult.Value;
        Assert.NotNull(value);
    }

    [Fact]
    public async Task ValidateTenant_WithValidTenant_ReturnsOkWithTrueResult()
    {
        // Arrange
        var tenantId = 1;
        _mockMigrationService
            .Setup(x => x.ValidateTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ValidateTenant(tenantId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ValidateTenant_WithInvalidTenant_ReturnsOkWithFalseResult()
    {
        // Arrange
        var tenantId = 999;
        _mockMigrationService
            .Setup(x => x.ValidateTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.ValidateTenant(tenantId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ValidateTenant_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var tenantId = 1;
        _mockMigrationService
            .Setup(x => x.ValidateTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _controller.ValidateTenant(tenantId, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
    }

    #endregion

    #region EnsureCodeReviewDocumentType Tests

    [Fact]
    public async Task EnsureCodeReviewDocumentType_WithSuccess_ReturnsOkWithDocumentTypeId()
    {
        // Arrange
        var expectedDocumentTypeId = 5;
        _mockMigrationService
            .Setup(x => x.EnsureCodeReviewDocumentTypeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDocumentTypeId);

        // Act
        var result = await _controller.EnsureCodeReviewDocumentType(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task EnsureCodeReviewDocumentType_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _mockMigrationService
            .Setup(x => x.EnsureCodeReviewDocumentTypeAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _controller.EnsureCodeReviewDocumentType(CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
    }

    #endregion
}
