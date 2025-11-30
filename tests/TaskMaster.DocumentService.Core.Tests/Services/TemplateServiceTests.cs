using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Interfaces;
using TaskMaster.DocumentService.Core.Services;

namespace TaskMaster.DocumentService.Core.Tests.Services;

/// <summary>
/// Unit tests for TemplateService.
/// </summary>
public class TemplateServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IBlobStorageService> _mockBlobStorageService;
    private readonly Mock<IDocumentService> _mockDocumentService;
    private readonly Mock<ILogger<TemplateService>> _mockLogger;
    private readonly Mock<IOptions<BlobStorageOptions>> _mockOptions;
    private readonly Mock<IDocumentTemplateRepository> _mockTemplateRepository;
    private readonly Mock<ITenantRepository> _mockTenantRepository;
    private readonly Mock<IDocumentTypeRepository> _mockDocumentTypeRepository;
    private readonly BlobStorageOptions _options;
    private readonly TemplateService _templateService;

    public TemplateServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockBlobStorageService = new Mock<IBlobStorageService>();
        _mockDocumentService = new Mock<IDocumentService>();
        _mockLogger = new Mock<ILogger<TemplateService>>();
        _mockTemplateRepository = new Mock<IDocumentTemplateRepository>();
        _mockTenantRepository = new Mock<ITenantRepository>();
        _mockDocumentTypeRepository = new Mock<IDocumentTypeRepository>();

        _options = new BlobStorageOptions
        {
            ConnectionString = "UseDevelopmentStorage=true",
            DefaultContainerName = "documents"
        };
        _mockOptions = new Mock<IOptions<BlobStorageOptions>>();
        _mockOptions.Setup(x => x.Value).Returns(_options);

        _mockUnitOfWork.Setup(x => x.DocumentTemplates).Returns(_mockTemplateRepository.Object);
        _mockUnitOfWork.Setup(x => x.Tenants).Returns(_mockTenantRepository.Object);
        _mockUnitOfWork.Setup(x => x.DocumentTypes).Returns(_mockDocumentTypeRepository.Object);

        _templateService = new TemplateService(
            _mockUnitOfWork.Object,
            _mockBlobStorageService.Object,
            _mockDocumentService.Object,
            _mockLogger.Object,
            _mockOptions.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange & Act
        var service = new TemplateService(
            _mockUnitOfWork.Object,
            _mockBlobStorageService.Object,
            _mockDocumentService.Object,
            _mockLogger.Object,
            _mockOptions.Object);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithNullUnitOfWork_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new TemplateService(null!, _mockBlobStorageService.Object, _mockDocumentService.Object, _mockLogger.Object, _mockOptions.Object));

        Assert.Equal("unitOfWork", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullBlobStorageService_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new TemplateService(_mockUnitOfWork.Object, null!, _mockDocumentService.Object, _mockLogger.Object, _mockOptions.Object));

        Assert.Equal("blobStorageService", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullDocumentService_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new TemplateService(_mockUnitOfWork.Object, _mockBlobStorageService.Object, null!, _mockLogger.Object, _mockOptions.Object));

        Assert.Equal("documentService", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new TemplateService(_mockUnitOfWork.Object, _mockBlobStorageService.Object, _mockDocumentService.Object, null!, _mockOptions.Object));

        Assert.Equal("logger", exception.ParamName);
    }

    #endregion

    #region CreateTemplateAsync Tests

    [Fact]
    public async Task CreateTemplateAsync_WithValidData_ShouldCreateTemplate()
    {
        // Arrange
        var tenantId = 1;
        var documentTypeId = 1;
        var name = "Test Template";
        var description = "Test Description";
        var fileName = "template.html";
        var contentType = "text/html";
        var availableVariables = "[\"customerName\", \"date\"]";
        var category = "Invoices";
        var createdBy = "testuser";

        var tenant = new Tenant { Id = tenantId, Name = "Test Tenant" };
        var documentType = new DocumentType { Id = documentTypeId, Name = "Invoice" };

        _mockTenantRepository.Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        _mockDocumentTypeRepository.Setup(x => x.GetByIdAsync(documentTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documentType);
        _mockBlobStorageService.Setup(x => x.UploadAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://blob.storage/template");

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Template content"));

        // Act
        var result = await _templateService.CreateTemplateAsync(
            tenantId,
            documentTypeId,
            name,
            description,
            stream,
            fileName,
            contentType,
            availableVariables,
            null,
            category,
            createdBy);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(name, result.Name);
        Assert.Equal(tenantId, result.TenantId);
        Assert.Equal(documentTypeId, result.DocumentTypeId);
        Assert.Equal(category, result.Category);
        Assert.True(result.IsActive);
        _mockTemplateRepository.Verify(x => x.AddAsync(It.IsAny<DocumentTemplate>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateTemplateAsync_WithInvalidName_ShouldThrowArgumentException(string invalidName)
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _templateService.CreateTemplateAsync(1, 1, invalidName, null, stream, "file.html", "text/html", null, null, null, "user"));
    }

    [Fact]
    public async Task CreateTemplateAsync_WithNonExistentTenant_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockTenantRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        using var stream = new MemoryStream();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _templateService.CreateTemplateAsync(1, 1, "Template", null, stream, "file.html", "text/html", null, null, null, "user"));
    }

    [Fact]
    public async Task CreateTemplateAsync_WithNonExistentDocumentType_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var tenant = new Tenant { Id = 1, Name = "Test Tenant" };
        _mockTenantRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        _mockDocumentTypeRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentType?)null);

        using var stream = new MemoryStream();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _templateService.CreateTemplateAsync(1, 1, "Template", null, stream, "file.html", "text/html", null, null, null, "user"));
    }

    #endregion

    #region GetTemplateByIdAsync Tests

    [Fact]
    public async Task GetTemplateByIdAsync_WithExistingTemplate_ShouldReturnTemplate()
    {
        // Arrange
        var templateId = 1L;
        var template = new DocumentTemplate { Id = templateId, Name = "Test Template" };
        _mockTemplateRepository.Setup(x => x.GetByIdAsync(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        // Act
        var result = await _templateService.GetTemplateByIdAsync(templateId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(templateId, result.Id);
    }

    [Fact]
    public async Task GetTemplateByIdAsync_WithNonExistentTemplate_ShouldReturnNull()
    {
        // Arrange
        _mockTemplateRepository.Setup(x => x.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentTemplate?)null);

        // Act
        var result = await _templateService.GetTemplateByIdAsync(999L);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetTemplatesByTenantAsync Tests

    [Fact]
    public async Task GetTemplatesByTenantAsync_ShouldReturnTemplates()
    {
        // Arrange
        var tenantId = 1;
        var templates = new List<DocumentTemplate>
        {
            new DocumentTemplate { Id = 1, TenantId = tenantId, Name = "Template 1" },
            new DocumentTemplate { Id = 2, TenantId = tenantId, Name = "Template 2" }
        };
        _mockTemplateRepository.Setup(x => x.GetByTenantIdAsync(tenantId, false, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates);

        // Act
        var result = await _templateService.GetTemplatesByTenantAsync(tenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    #endregion

    #region UpdateTemplateMetadataAsync Tests

    [Fact]
    public async Task UpdateTemplateMetadataAsync_WithValidData_ShouldUpdateTemplate()
    {
        // Arrange
        var templateId = 1L;
        var template = new DocumentTemplate
        {
            Id = templateId,
            Name = "Old Name",
            IsDeleted = false
        };
        _mockTemplateRepository.Setup(x => x.GetByIdAsync(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        // Act
        var result = await _templateService.UpdateTemplateMetadataAsync(
            templateId,
            "New Name",
            "New Description",
            null,
            null,
            null,
            true,
            "user");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Name", result.Name);
        Assert.Equal("New Description", result.Description);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTemplateMetadataAsync_WithDeletedTemplate_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var template = new DocumentTemplate { Id = 1, IsDeleted = true };
        _mockTemplateRepository.Setup(x => x.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _templateService.UpdateTemplateMetadataAsync(1, "Name", null, null, null, null, null, "user"));
    }

    #endregion

    #region DownloadTemplateAsync Tests

    [Fact]
    public async Task DownloadTemplateAsync_WithValidTemplate_ShouldReturnStream()
    {
        // Arrange
        var templateId = 1L;
        var template = new DocumentTemplate
        {
            Id = templateId,
            BlobPath = "templates/test.html",
            IsDeleted = false
        };
        _mockTemplateRepository.Setup(x => x.GetByIdAsync(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        var expectedStream = new MemoryStream();
        _mockBlobStorageService.Setup(x => x.DownloadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStream);

        // Act
        var result = await _templateService.DownloadTemplateAsync(templateId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedStream, result);
    }

    [Fact]
    public async Task DownloadTemplateAsync_WithDeletedTemplate_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var template = new DocumentTemplate { Id = 1, IsDeleted = true };
        _mockTemplateRepository.Setup(x => x.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _templateService.DownloadTemplateAsync(1));
    }

    #endregion

    #region GenerateDocumentFromTemplateAsync Tests

    [Fact]
    public async Task GenerateDocumentFromTemplateAsync_WithTextTemplate_ShouldPerformVariableSubstitution()
    {
        // Arrange
        var templateId = 1L;
        var template = new DocumentTemplate
        {
            Id = templateId,
            TenantId = 1,
            DocumentTypeId = 1,
            BlobPath = "templates/test.html",
            MimeType = "text/html",
            OriginalFileName = "template.html",
            IsDeleted = false,
            IsActive = true
        };
        _mockTemplateRepository.Setup(x => x.GetByIdAsync(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        var templateContent = "Hello {{customerName}}, your invoice date is {{invoiceDate}}.";
        var templateStream = new MemoryStream(Encoding.UTF8.GetBytes(templateContent));
        _mockBlobStorageService.Setup(x => x.DownloadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(templateStream);

        var variables = new Dictionary<string, string>
        {
            { "customerName", "John Doe" },
            { "invoiceDate", "2025-11-30" }
        };

        var expectedDocument = new Document { Id = 1, Title = "Test Document" };
        _mockDocumentService.Setup(x => x.CreateDocumentAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDocument);

        // Act
        var result = await _templateService.GenerateDocumentFromTemplateAsync(
            templateId,
            "Test Document",
            "Test Description",
            variables,
            null,
            null,
            "user");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedDocument.Id, result.Id);
        _mockDocumentService.Verify(x => x.CreateDocumentAsync(
            template.TenantId,
            template.DocumentTypeId,
            "Test Document",
            "Test Description",
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            template.MimeType,
            null,
            null,
            "user",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateDocumentFromTemplateAsync_WithDeletedTemplate_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var template = new DocumentTemplate { Id = 1, IsDeleted = true };
        _mockTemplateRepository.Setup(x => x.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        var variables = new Dictionary<string, string>();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _templateService.GenerateDocumentFromTemplateAsync(1, "Title", null, variables, null, null, "user"));
    }

    [Fact]
    public async Task GenerateDocumentFromTemplateAsync_WithInactiveTemplate_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var template = new DocumentTemplate { Id = 1, IsDeleted = false, IsActive = false };
        _mockTemplateRepository.Setup(x => x.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        var variables = new Dictionary<string, string>();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _templateService.GenerateDocumentFromTemplateAsync(1, "Title", null, variables, null, null, "user"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GenerateDocumentFromTemplateAsync_WithInvalidTitle_ShouldThrowArgumentException(string invalidTitle)
    {
        // Arrange
        var variables = new Dictionary<string, string>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _templateService.GenerateDocumentFromTemplateAsync(1, invalidTitle, null, variables, null, null, "user"));
    }

    #endregion

    #region DeleteTemplateAsync Tests

    [Fact]
    public async Task DeleteTemplateAsync_WithValidTemplate_ShouldSoftDeleteTemplate()
    {
        // Arrange
        var templateId = 1L;
        var deletedBy = "testuser";

        // Act
        await _templateService.DeleteTemplateAsync(templateId, deletedBy);

        // Assert
        _mockTemplateRepository.Verify(x => x.SoftDeleteAsync(templateId, deletedBy, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DeleteTemplateAsync_WithInvalidDeletedBy_ShouldThrowArgumentException(string invalidDeletedBy)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _templateService.DeleteTemplateAsync(1, invalidDeletedBy));
    }

    #endregion

    #region RestoreTemplateAsync Tests

    [Fact]
    public async Task RestoreTemplateAsync_ShouldRestoreTemplate()
    {
        // Arrange
        var templateId = 1L;

        // Act
        await _templateService.RestoreTemplateAsync(templateId);

        // Assert
        _mockTemplateRepository.Verify(x => x.RestoreAsync(templateId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region PermanentlyDeleteTemplateAsync Tests

    [Fact]
    public async Task PermanentlyDeleteTemplateAsync_WithValidTemplate_ShouldDeletePermanently()
    {
        // Arrange
        var templateId = 1L;
        var template = new DocumentTemplate
        {
            Id = templateId,
            BlobPath = "templates/test.html"
        };
        _mockTemplateRepository.Setup(x => x.GetByIdAsync(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _mockBlobStorageService.Setup(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _templateService.PermanentlyDeleteTemplateAsync(templateId);

        // Assert
        _mockBlobStorageService.Verify(x => x.DeleteAsync(_options.DefaultContainerName, template.BlobPath, It.IsAny<CancellationToken>()), Times.Once);
        _mockTemplateRepository.Verify(x => x.Remove(template), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PermanentlyDeleteTemplateAsync_WithNonExistentTemplate_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockTemplateRepository.Setup(x => x.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentTemplate?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _templateService.PermanentlyDeleteTemplateAsync(1));
    }

    #endregion

    #region SearchTemplatesAsync Tests

    [Fact]
    public async Task SearchTemplatesAsync_ShouldReturnMatchingTemplates()
    {
        // Arrange
        var tenantId = 1;
        var searchTerm = "invoice";
        var templates = new List<DocumentTemplate>
        {
            new DocumentTemplate { Id = 1, Name = "Invoice Template" },
            new DocumentTemplate { Id = 2, Name = "Invoice v2" }
        };
        _mockTemplateRepository.Setup(x => x.SearchByNameAsync(tenantId, searchTerm, It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates);

        // Act
        var result = await _templateService.SearchTemplatesAsync(tenantId, searchTerm);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    #endregion

    #region GetTemplatesByCategoryAsync Tests

    [Fact]
    public async Task GetTemplatesByCategoryAsync_ShouldReturnTemplatesInCategory()
    {
        // Arrange
        var tenantId = 1;
        var category = "Invoices";
        var templates = new List<DocumentTemplate>
        {
            new DocumentTemplate { Id = 1, Category = category },
            new DocumentTemplate { Id = 2, Category = category }
        };
        _mockTemplateRepository.Setup(x => x.GetByCategoryAsync(tenantId, category, It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates);

        // Act
        var result = await _templateService.GetTemplatesByCategoryAsync(tenantId, category);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    #endregion
}
