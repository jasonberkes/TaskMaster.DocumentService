using Microsoft.Extensions.Logging;
using Moq;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Interfaces;
using TaskMaster.DocumentService.Core.Services;

namespace TaskMaster.DocumentService.Core.Tests.Services;

/// <summary>
/// Unit tests for DocumentTypeService.
/// </summary>
public class DocumentTypeServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IDocumentTypeRepository> _mockDocumentTypeRepository;
    private readonly Mock<ILogger<DocumentTypeService>> _mockLogger;
    private readonly DocumentTypeService _documentTypeService;

    public DocumentTypeServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockDocumentTypeRepository = new Mock<IDocumentTypeRepository>();
        _mockLogger = new Mock<ILogger<DocumentTypeService>>();

        // Setup UnitOfWork to return the mocked repository
        _mockUnitOfWork.Setup(x => x.DocumentTypes).Returns(_mockDocumentTypeRepository.Object);

        _documentTypeService = new DocumentTypeService(_mockUnitOfWork.Object, _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange & Act
        var service = new DocumentTypeService(_mockUnitOfWork.Object, _mockLogger.Object);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithNullUnitOfWork_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new DocumentTypeService(null!, _mockLogger.Object));

        Assert.Equal("unitOfWork", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new DocumentTypeService(_mockUnitOfWork.Object, null!));

        Assert.Equal("logger", exception.ParamName);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnDocumentType()
    {
        // Arrange
        var documentTypeId = 1;
        var expectedDocumentType = new DocumentType
        {
            Id = documentTypeId,
            Name = "Invoice",
            DisplayName = "Invoice Document",
            Description = "Invoice documents",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _mockDocumentTypeRepository
            .Setup(x => x.GetByIdAsync(documentTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDocumentType);

        // Act
        var result = await _documentTypeService.GetByIdAsync(documentTypeId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedDocumentType.Id, result.Id);
        Assert.Equal(expectedDocumentType.Name, result.Name);
        Assert.Equal(expectedDocumentType.DisplayName, result.DisplayName);
        _mockDocumentTypeRepository.Verify(x => x.GetByIdAsync(documentTypeId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ShouldReturnNull()
    {
        // Arrange
        var documentTypeId = 999;
        _mockDocumentTypeRepository
            .Setup(x => x.GetByIdAsync(documentTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentType?)null);

        // Act
        var result = await _documentTypeService.GetByIdAsync(documentTypeId);

        // Assert
        Assert.Null(result);
        _mockDocumentTypeRepository.Verify(x => x.GetByIdAsync(documentTypeId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WithZeroId_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _documentTypeService.GetByIdAsync(0));

        Assert.Equal("id", exception.ParamName);
        Assert.Contains("must be greater than zero", exception.Message);
    }

    [Fact]
    public async Task GetByIdAsync_WithNegativeId_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _documentTypeService.GetByIdAsync(-1));

        Assert.Equal("id", exception.ParamName);
    }

    #endregion

    #region GetByNameAsync Tests

    [Fact]
    public async Task GetByNameAsync_WithValidName_ShouldReturnDocumentType()
    {
        // Arrange
        var name = "Invoice";
        var expectedDocumentType = new DocumentType
        {
            Id = 1,
            Name = name,
            DisplayName = "Invoice Document",
            IsActive = true
        };

        _mockDocumentTypeRepository
            .Setup(x => x.GetByNameAsync(name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDocumentType);

        // Act
        var result = await _documentTypeService.GetByNameAsync(name);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedDocumentType.Name, result.Name);
        _mockDocumentTypeRepository.Verify(x => x.GetByNameAsync(name, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByNameAsync_WithNonExistentName_ShouldReturnNull()
    {
        // Arrange
        var name = "NonExistent";
        _mockDocumentTypeRepository
            .Setup(x => x.GetByNameAsync(name, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentType?)null);

        // Act
        var result = await _documentTypeService.GetByNameAsync(name);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetByNameAsync_WithInvalidName_ShouldThrowArgumentException(string? name)
    {
        // Arrange & Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _documentTypeService.GetByNameAsync(name!));

        Assert.Equal("name", exception.ParamName);
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllDocumentTypes()
    {
        // Arrange
        var documentTypes = new List<DocumentType>
        {
            new DocumentType { Id = 1, Name = "Invoice", DisplayName = "Invoice", IsActive = true },
            new DocumentType { Id = 2, Name = "Receipt", DisplayName = "Receipt", IsActive = true },
            new DocumentType { Id = 3, Name = "Contract", DisplayName = "Contract", IsActive = false }
        };

        _mockDocumentTypeRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(documentTypes);

        // Act
        var result = await _documentTypeService.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
        _mockDocumentTypeRepository.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithNoDocumentTypes_ShouldReturnEmptyCollection()
    {
        // Arrange
        _mockDocumentTypeRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DocumentType>());

        // Act
        var result = await _documentTypeService.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region GetActiveDocumentTypesAsync Tests

    [Fact]
    public async Task GetActiveDocumentTypesAsync_ShouldReturnOnlyActiveDocumentTypes()
    {
        // Arrange
        var activeDocumentTypes = new List<DocumentType>
        {
            new DocumentType { Id = 1, Name = "Invoice", DisplayName = "Invoice", IsActive = true },
            new DocumentType { Id = 2, Name = "Receipt", DisplayName = "Receipt", IsActive = true }
        };

        _mockDocumentTypeRepository
            .Setup(x => x.GetActiveDocumentTypesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeDocumentTypes);

        // Act
        var result = await _documentTypeService.GetActiveDocumentTypesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, dt => Assert.True(dt.IsActive));
        _mockDocumentTypeRepository.Verify(x => x.GetActiveDocumentTypesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetIndexableTypesAsync Tests

    [Fact]
    public async Task GetIndexableTypesAsync_ShouldReturnOnlyIndexableTypes()
    {
        // Arrange
        var indexableTypes = new List<DocumentType>
        {
            new DocumentType { Id = 1, Name = "Invoice", DisplayName = "Invoice", IsContentIndexed = true },
            new DocumentType { Id = 2, Name = "Receipt", DisplayName = "Receipt", IsContentIndexed = true }
        };

        _mockDocumentTypeRepository
            .Setup(x => x.GetIndexableTypesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(indexableTypes);

        // Act
        var result = await _documentTypeService.GetIndexableTypesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, dt => Assert.True(dt.IsContentIndexed));
    }

    #endregion

    #region GetTypesWithExtensionTablesAsync Tests

    [Fact]
    public async Task GetTypesWithExtensionTablesAsync_ShouldReturnOnlyTypesWithExtensionTables()
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
                ExtensionTableName = "InvoiceExtension"
            }
        };

        _mockDocumentTypeRepository
            .Setup(x => x.GetTypesWithExtensionTablesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(typesWithExtensions);

        // Act
        var result = await _documentTypeService.GetTypesWithExtensionTablesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.All(result, dt => Assert.True(dt.HasExtensionTable));
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidDocumentType_ShouldCreateAndReturnDocumentType()
    {
        // Arrange
        var documentType = new DocumentType
        {
            Name = "Invoice",
            DisplayName = "Invoice Document",
            Description = "Invoice documents",
            IsActive = true
        };

        _mockDocumentTypeRepository
            .Setup(x => x.GetByNameAsync(documentType.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentType?)null);

        _mockDocumentTypeRepository
            .Setup(x => x.AddAsync(It.IsAny<DocumentType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentType dt, CancellationToken ct) =>
            {
                dt.Id = 1;
                return dt;
            });

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _documentTypeService.CreateAsync(documentType);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(documentType.Name, result.Name);
        Assert.NotEqual(default(DateTime), result.CreatedAt);
        _mockDocumentTypeRepository.Verify(x => x.AddAsync(It.IsAny<DocumentType>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithNullDocumentType_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _documentTypeService.CreateAsync(null!));

        Assert.Equal("documentType", exception.ParamName);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyName_ShouldThrowArgumentException()
    {
        // Arrange
        var documentType = new DocumentType
        {
            Name = "",
            DisplayName = "Test"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _documentTypeService.CreateAsync(documentType));

        Assert.Equal("documentType", exception.ParamName);
        Assert.Contains("name", exception.Message.ToLower());
    }

    [Fact]
    public async Task CreateAsync_WithEmptyDisplayName_ShouldThrowArgumentException()
    {
        // Arrange
        var documentType = new DocumentType
        {
            Name = "Test",
            DisplayName = ""
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _documentTypeService.CreateAsync(documentType));

        Assert.Equal("documentType", exception.ParamName);
        Assert.Contains("display name", exception.Message.ToLower());
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateName_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var documentType = new DocumentType
        {
            Name = "Invoice",
            DisplayName = "Invoice Document"
        };

        var existingDocumentType = new DocumentType
        {
            Id = 1,
            Name = "Invoice",
            DisplayName = "Existing Invoice"
        };

        _mockDocumentTypeRepository
            .Setup(x => x.GetByNameAsync(documentType.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingDocumentType);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _documentTypeService.CreateAsync(documentType));

        Assert.Contains("already exists", exception.Message);
        _mockDocumentTypeRepository.Verify(x => x.AddAsync(It.IsAny<DocumentType>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithExtensionTableEnabledButNoTableName_ShouldThrowArgumentException()
    {
        // Arrange
        var documentType = new DocumentType
        {
            Name = "Invoice",
            DisplayName = "Invoice Document",
            HasExtensionTable = true,
            ExtensionTableName = null
        };

        _mockDocumentTypeRepository
            .Setup(x => x.GetByNameAsync(documentType.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentType?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _documentTypeService.CreateAsync(documentType));

        Assert.Equal("documentType", exception.ParamName);
        Assert.Contains("Extension table name is required", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_WithTableNameButExtensionDisabled_ShouldClearTableName()
    {
        // Arrange
        var documentType = new DocumentType
        {
            Name = "Invoice",
            DisplayName = "Invoice Document",
            HasExtensionTable = false,
            ExtensionTableName = "SomeTable"
        };

        _mockDocumentTypeRepository
            .Setup(x => x.GetByNameAsync(documentType.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentType?)null);

        _mockDocumentTypeRepository
            .Setup(x => x.AddAsync(It.IsAny<DocumentType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentType dt, CancellationToken ct) =>
            {
                dt.Id = 1;
                return dt;
            });

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _documentTypeService.CreateAsync(documentType);

        // Assert
        Assert.Null(result.ExtensionTableName);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidDocumentType_ShouldUpdateAndReturnDocumentType()
    {
        // Arrange
        var existingDocumentType = new DocumentType
        {
            Id = 1,
            Name = "Invoice",
            DisplayName = "Old Invoice",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var updatedDocumentType = new DocumentType
        {
            Id = 1,
            Name = "Invoice",
            DisplayName = "Updated Invoice",
            Description = "Updated description",
            IsActive = true
        };

        _mockDocumentTypeRepository
            .Setup(x => x.GetByIdAsync(updatedDocumentType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingDocumentType);

        _mockDocumentTypeRepository
            .Setup(x => x.GetByNameAsync(updatedDocumentType.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingDocumentType);

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _documentTypeService.UpdateAsync(updatedDocumentType);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(updatedDocumentType.DisplayName, result.DisplayName);
        Assert.Equal(existingDocumentType.CreatedAt, result.CreatedAt);
        _mockDocumentTypeRepository.Verify(x => x.Update(It.IsAny<DocumentType>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNullDocumentType_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _documentTypeService.UpdateAsync(null!));

        Assert.Equal("documentType", exception.ParamName);
    }

    [Fact]
    public async Task UpdateAsync_WithZeroId_ShouldThrowArgumentException()
    {
        // Arrange
        var documentType = new DocumentType
        {
            Id = 0,
            Name = "Test",
            DisplayName = "Test"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _documentTypeService.UpdateAsync(documentType));

        Assert.Equal("documentType", exception.ParamName);
        Assert.Contains("must be greater than zero", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentId_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var documentType = new DocumentType
        {
            Id = 999,
            Name = "Test",
            DisplayName = "Test"
        };

        _mockDocumentTypeRepository
            .Setup(x => x.GetByIdAsync(documentType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentType?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _documentTypeService.UpdateAsync(documentType));

        Assert.Contains("does not exist", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_WithDuplicateName_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var existingDocumentType = new DocumentType
        {
            Id = 1,
            Name = "Invoice",
            DisplayName = "Invoice"
        };

        var anotherDocumentType = new DocumentType
        {
            Id = 2,
            Name = "Invoice",
            DisplayName = "Another Invoice"
        };

        var updatedDocumentType = new DocumentType
        {
            Id = 1,
            Name = "Invoice",
            DisplayName = "Updated"
        };

        _mockDocumentTypeRepository
            .Setup(x => x.GetByIdAsync(updatedDocumentType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingDocumentType);

        _mockDocumentTypeRepository
            .Setup(x => x.GetByNameAsync(updatedDocumentType.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(anotherDocumentType);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _documentTypeService.UpdateAsync(updatedDocumentType));

        Assert.Contains("already exists", exception.Message);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_ShouldDeleteAndReturnTrue()
    {
        // Arrange
        var documentTypeId = 1;
        var documentType = new DocumentType
        {
            Id = documentTypeId,
            Name = "Invoice",
            DisplayName = "Invoice",
            Documents = new List<Document>()
        };

        _mockDocumentTypeRepository
            .Setup(x => x.GetByIdAsync(documentTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documentType);

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _documentTypeService.DeleteAsync(documentTypeId);

        // Assert
        Assert.True(result);
        _mockDocumentTypeRepository.Verify(x => x.Remove(documentType), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentId_ShouldReturnFalse()
    {
        // Arrange
        var documentTypeId = 999;
        _mockDocumentTypeRepository
            .Setup(x => x.GetByIdAsync(documentTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentType?)null);

        // Act
        var result = await _documentTypeService.DeleteAsync(documentTypeId);

        // Assert
        Assert.False(result);
        _mockDocumentTypeRepository.Verify(x => x.Remove(It.IsAny<DocumentType>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithZeroId_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _documentTypeService.DeleteAsync(0));

        Assert.Equal("id", exception.ParamName);
    }

    [Fact]
    public async Task DeleteAsync_WithDocumentsInUse_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var documentTypeId = 1;
        var documentType = new DocumentType
        {
            Id = documentTypeId,
            Name = "Invoice",
            DisplayName = "Invoice",
            Documents = new List<Document>
            {
                new Document { Id = 1 },
                new Document { Id = 2 }
            }
        };

        _mockDocumentTypeRepository
            .Setup(x => x.GetByIdAsync(documentTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documentType);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _documentTypeService.DeleteAsync(documentTypeId));

        Assert.Contains("is used by", exception.Message);
        Assert.Contains("2", exception.Message);
        _mockDocumentTypeRepository.Verify(x => x.Remove(It.IsAny<DocumentType>()), Times.Never);
    }

    #endregion

    #region ExistsByNameAsync Tests

    [Fact]
    public async Task ExistsByNameAsync_WithExistingName_ShouldReturnTrue()
    {
        // Arrange
        var name = "Invoice";
        var documentType = new DocumentType { Id = 1, Name = name, DisplayName = "Invoice" };

        _mockDocumentTypeRepository
            .Setup(x => x.GetByNameAsync(name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documentType);

        // Act
        var result = await _documentTypeService.ExistsByNameAsync(name);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsByNameAsync_WithNonExistentName_ShouldReturnFalse()
    {
        // Arrange
        var name = "NonExistent";
        _mockDocumentTypeRepository
            .Setup(x => x.GetByNameAsync(name, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentType?)null);

        // Act
        var result = await _documentTypeService.ExistsByNameAsync(name);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExistsByNameAsync_WithInvalidName_ShouldThrowArgumentException(string? name)
    {
        // Arrange & Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _documentTypeService.ExistsByNameAsync(name!));

        Assert.Equal("name", exception.ParamName);
    }

    #endregion
}
