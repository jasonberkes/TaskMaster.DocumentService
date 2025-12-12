using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TaskMaster.DocumentService.Api.Controllers;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Interfaces;
using Xunit;

namespace TaskMaster.DocumentService.Api.Tests.Controllers;

/// <summary>
/// Unit tests for <see cref="DocumentTypesController"/>.
/// </summary>
public class DocumentTypesControllerTests
{
    private readonly Mock<IDocumentTypeService> _documentTypeService;
    private readonly Mock<ILogger<DocumentTypesController>> _logger;
    private readonly DocumentTypesController _controller;

    public DocumentTypesControllerTests()
    {
        _documentTypeService = new Mock<IDocumentTypeService>();
        _logger = new Mock<ILogger<DocumentTypesController>>();
        _controller = new DocumentTypesController(_documentTypeService.Object, _logger.Object);

        // Setup default authenticated user
        var claims = new List<Claim>
        {
            new("TenantId", "1"),
            new("TenantName", "Test Tenant"),
            new(ClaimTypes.Name, "testuser"),
            new("AuthenticationType", "Bearer")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullService_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new DocumentTypesController(null!, _logger.Object));
        Assert.Equal("documentTypeService", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new DocumentTypesController(_documentTypeService.Object, null!));
        Assert.Equal("logger", exception.ParamName);
    }

    #endregion

    #region GetAllDocumentTypes Tests

    [Fact]
    public async Task GetAllDocumentTypes_ReturnsOkResultWithDocumentTypes()
    {
        // Arrange
        var documentTypes = new List<DocumentType>
        {
            new DocumentType
            {
                Id = 1,
                Name = "Invoice",
                DisplayName = "Invoice Document",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new DocumentType
            {
                Id = 2,
                Name = "Receipt",
                DisplayName = "Receipt Document",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        _documentTypeService
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(documentTypes);

        // Act
        var result = await _controller.GetAllDocumentTypes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetAllDocumentTypes_WithEmptyList_ReturnsOkResultWithEmptyList()
    {
        // Arrange
        _documentTypeService
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DocumentType>());

        // Act
        var result = await _controller.GetAllDocumentTypes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetAllDocumentTypes_WithException_ReturnsInternalServerError()
    {
        // Arrange
        _documentTypeService
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _controller.GetAllDocumentTypes();

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
    }

    #endregion

    #region GetActiveDocumentTypes Tests

    [Fact]
    public async Task GetActiveDocumentTypes_ReturnsOkResultWithActiveDocumentTypes()
    {
        // Arrange
        var activeDocumentTypes = new List<DocumentType>
        {
            new DocumentType
            {
                Id = 1,
                Name = "Invoice",
                DisplayName = "Invoice",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        _documentTypeService
            .Setup(x => x.GetActiveDocumentTypesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeDocumentTypes);

        // Act
        var result = await _controller.GetActiveDocumentTypes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region GetIndexableDocumentTypes Tests

    [Fact]
    public async Task GetIndexableDocumentTypes_ReturnsOkResultWithIndexableTypes()
    {
        // Arrange
        var indexableTypes = new List<DocumentType>
        {
            new DocumentType
            {
                Id = 1,
                Name = "Invoice",
                DisplayName = "Invoice",
                IsContentIndexed = true,
                IsActive = true
            }
        };

        _documentTypeService
            .Setup(x => x.GetIndexableTypesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(indexableTypes);

        // Act
        var result = await _controller.GetIndexableDocumentTypes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region GetDocumentTypesWithExtensionTables Tests

    [Fact]
    public async Task GetDocumentTypesWithExtensionTables_ReturnsOkResultWithTypesWithExtensions()
    {
        // Arrange
        var typesWithExtensions = new List<DocumentType>
        {
            new DocumentType
            {
                Id = 1,
                Name = "Invoice",
                DisplayName = "Invoice",
                HasExtensionTable = true,
                ExtensionTableName = "InvoiceExtension",
                IsActive = true
            }
        };

        _documentTypeService
            .Setup(x => x.GetTypesWithExtensionTablesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(typesWithExtensions);

        // Act
        var result = await _controller.GetDocumentTypesWithExtensionTables();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region GetDocumentTypeById Tests

    [Fact]
    public async Task GetDocumentTypeById_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var documentTypeId = 1;
        var documentType = new DocumentType
        {
            Id = documentTypeId,
            Name = "Invoice",
            DisplayName = "Invoice Document",
            Description = "Invoice documents",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _documentTypeService
            .Setup(x => x.GetByIdAsync(documentTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documentType);

        // Act
        var result = await _controller.GetDocumentTypeById(documentTypeId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetDocumentTypeById_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var documentTypeId = 999;
        _documentTypeService
            .Setup(x => x.GetByIdAsync(documentTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentType?)null);

        // Act
        var result = await _controller.GetDocumentTypeById(documentTypeId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task GetDocumentTypeById_WithInvalidId_ReturnsBadRequest()
    {
        // Arrange
        var documentTypeId = 0;
        _documentTypeService
            .Setup(x => x.GetByIdAsync(documentTypeId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Document type ID must be greater than zero.", "id"));

        // Act
        var result = await _controller.GetDocumentTypeById(documentTypeId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    #endregion

    #region GetDocumentTypeByName Tests

    [Fact]
    public async Task GetDocumentTypeByName_WithValidName_ReturnsOkResult()
    {
        // Arrange
        var name = "Invoice";
        var documentType = new DocumentType
        {
            Id = 1,
            Name = name,
            DisplayName = "Invoice Document",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _documentTypeService
            .Setup(x => x.GetByNameAsync(name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documentType);

        // Act
        var result = await _controller.GetDocumentTypeByName(name);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetDocumentTypeByName_WithNonExistentName_ReturnsNotFound()
    {
        // Arrange
        var name = "NonExistent";
        _documentTypeService
            .Setup(x => x.GetByNameAsync(name, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentType?)null);

        // Act
        var result = await _controller.GetDocumentTypeByName(name);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    #endregion

    #region CreateDocumentType Tests

    [Fact]
    public async Task CreateDocumentType_WithValidRequest_ReturnsCreatedAtAction()
    {
        // Arrange
        var request = new CreateDocumentTypeRequest
        {
            Name = "Invoice",
            DisplayName = "Invoice Document",
            Description = "Invoice documents",
            IsActive = true
        };

        var createdDocumentType = new DocumentType
        {
            Id = 1,
            Name = request.Name,
            DisplayName = request.DisplayName,
            Description = request.Description,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _documentTypeService
            .Setup(x => x.CreateAsync(It.IsAny<DocumentType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDocumentType);

        // Act
        var result = await _controller.CreateDocumentType(request);

        // Assert
        var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.NotNull(createdAtActionResult.Value);
        Assert.Equal("GetDocumentTypeById", createdAtActionResult.ActionName);
    }

    [Fact]
    public async Task CreateDocumentType_WithEmptyName_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateDocumentTypeRequest
        {
            Name = "",
            DisplayName = "Test"
        };

        // Act
        var result = await _controller.CreateDocumentType(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task CreateDocumentType_WithEmptyDisplayName_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateDocumentTypeRequest
        {
            Name = "Test",
            DisplayName = ""
        };

        // Act
        var result = await _controller.CreateDocumentType(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task CreateDocumentType_WithDuplicateName_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateDocumentTypeRequest
        {
            Name = "Invoice",
            DisplayName = "Invoice Document"
        };

        _documentTypeService
            .Setup(x => x.CreateAsync(It.IsAny<DocumentType>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("A document type with name 'Invoice' already exists."));

        // Act
        var result = await _controller.CreateDocumentType(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task CreateDocumentType_WithExtensionTableButNoName_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateDocumentTypeRequest
        {
            Name = "Invoice",
            DisplayName = "Invoice Document",
            HasExtensionTable = true,
            ExtensionTableName = null
        };

        _documentTypeService
            .Setup(x => x.CreateAsync(It.IsAny<DocumentType>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Extension table name is required when HasExtensionTable is true."));

        // Act
        var result = await _controller.CreateDocumentType(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    #endregion

    #region UpdateDocumentType Tests

    [Fact]
    public async Task UpdateDocumentType_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var documentTypeId = 1;
        var request = new UpdateDocumentTypeRequest
        {
            Name = "Invoice",
            DisplayName = "Updated Invoice Document",
            Description = "Updated description",
            IsActive = true
        };

        var updatedDocumentType = new DocumentType
        {
            Id = documentTypeId,
            Name = request.Name,
            DisplayName = request.DisplayName,
            Description = request.Description,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _documentTypeService
            .Setup(x => x.UpdateAsync(It.IsAny<DocumentType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedDocumentType);

        // Act
        var result = await _controller.UpdateDocumentType(documentTypeId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task UpdateDocumentType_WithEmptyName_ReturnsBadRequest()
    {
        // Arrange
        var request = new UpdateDocumentTypeRequest
        {
            Name = "",
            DisplayName = "Test"
        };

        // Act
        var result = await _controller.UpdateDocumentType(1, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task UpdateDocumentType_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var documentTypeId = 999;
        var request = new UpdateDocumentTypeRequest
        {
            Name = "Invoice",
            DisplayName = "Invoice Document"
        };

        _documentTypeService
            .Setup(x => x.UpdateAsync(It.IsAny<DocumentType>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException($"Document type with ID {documentTypeId} does not exist."));

        // Act
        var result = await _controller.UpdateDocumentType(documentTypeId, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task UpdateDocumentType_WithDuplicateName_ReturnsBadRequest()
    {
        // Arrange
        var documentTypeId = 1;
        var request = new UpdateDocumentTypeRequest
        {
            Name = "Invoice",
            DisplayName = "Invoice Document"
        };

        _documentTypeService
            .Setup(x => x.UpdateAsync(It.IsAny<DocumentType>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("A document type with name 'Invoice' already exists."));

        // Act
        var result = await _controller.UpdateDocumentType(documentTypeId, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    #endregion

    #region DeleteDocumentType Tests

    [Fact]
    public async Task DeleteDocumentType_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var documentTypeId = 1;
        _documentTypeService
            .Setup(x => x.DeleteAsync(documentTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteDocumentType(documentTypeId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteDocumentType_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var documentTypeId = 999;
        _documentTypeService
            .Setup(x => x.DeleteAsync(documentTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteDocumentType(documentTypeId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task DeleteDocumentType_WithDocumentsInUse_ReturnsBadRequest()
    {
        // Arrange
        var documentTypeId = 1;
        _documentTypeService
            .Setup(x => x.DeleteAsync(documentTypeId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cannot delete document type 1 because it is used by 5 document(s)."));

        // Act
        var result = await _controller.DeleteDocumentType(documentTypeId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task DeleteDocumentType_WithInvalidId_ReturnsBadRequest()
    {
        // Arrange
        var documentTypeId = 0;
        _documentTypeService
            .Setup(x => x.DeleteAsync(documentTypeId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Document type ID must be greater than zero.", "id"));

        // Act
        var result = await _controller.DeleteDocumentType(documentTypeId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    #endregion
}
