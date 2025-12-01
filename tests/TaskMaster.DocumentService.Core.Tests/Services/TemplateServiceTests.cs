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
    private readonly Mock<ILogger<TemplateService>> _mockLogger;
    private readonly Mock<IOptions<BlobStorageOptions>> _mockOptions;
    private readonly Mock<ITemplateRepository> _mockTemplateRepository;
    private readonly Mock<ITenantRepository> _mockTenantRepository;
    private readonly Mock<IDocumentTypeRepository> _mockDocumentTypeRepository;
    private readonly Mock<IDocumentRepository> _mockDocumentRepository;
    private readonly BlobStorageOptions _options;
    private readonly TemplateService _templateService;

    public TemplateServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockBlobStorageService = new Mock<IBlobStorageService>();
        _mockLogger = new Mock<ILogger<TemplateService>>();
        _mockTemplateRepository = new Mock<ITemplateRepository>();
        _mockTenantRepository = new Mock<ITenantRepository>();
        _mockDocumentTypeRepository = new Mock<IDocumentTypeRepository>();
        _mockDocumentRepository = new Mock<IDocumentRepository>();

        _options = new BlobStorageOptions
        {
            ConnectionString = "UseDevelopmentStorage=true",
            DefaultContainerName = "documents"
        };
        _mockOptions = new Mock<IOptions<BlobStorageOptions>>();
        _mockOptions.Setup(x => x.Value).Returns(_options);

        _mockUnitOfWork.Setup(x => x.Templates).Returns(_mockTemplateRepository.Object);
        _mockUnitOfWork.Setup(x => x.Tenants).Returns(_mockTenantRepository.Object);
        _mockUnitOfWork.Setup(x => x.DocumentTypes).Returns(_mockDocumentTypeRepository.Object);
        _mockUnitOfWork.Setup(x => x.Documents).Returns(_mockDocumentRepository.Object);

        _templateService = new TemplateService(
            _mockUnitOfWork.Object,
            _mockBlobStorageService.Object,
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
            new TemplateService(null!, _mockBlobStorageService.Object, _mockLogger.Object, _mockOptions.Object));

        Assert.Equal("unitOfWork", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullBlobStorageService_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new TemplateService(_mockUnitOfWork.Object, null!, _mockLogger.Object, _mockOptions.Object));

        Assert.Equal("blobStorageService", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new TemplateService(_mockUnitOfWork.Object, _mockBlobStorageService.Object, null!, _mockOptions.Object));

        Assert.Equal("logger", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new TemplateService(_mockUnitOfWork.Object, _mockBlobStorageService.Object, _mockLogger.Object, null!));

        Assert.Equal("blobStorageOptions", exception.ParamName);
    }

    #endregion

    #region CreateTemplateAsync Tests

    [Fact]
    public async Task CreateTemplateAsync_WithValidData_ShouldCreateTemplate()
    {
        // Arrange
        var tenantId = 1;
        var documentTypeId = 1;
        var name = "Invoice Template";
        var description = "Standard invoice template";
        var content = "Invoice for {{customerName}}\nAmount: {{amount}}";
        var mimeType = "text/plain";
        var createdBy = "testuser";

        var tenant = new Tenant { Id = tenantId, Name = "Test Tenant", Slug = "test-tenant", TenantType = "Organization" };
        var documentType = new DocumentType { Id = documentTypeId, Name = "invoice", DisplayName = "Invoice" };

        var variables = new List<TemplateVariable>
        {
            new() { Name = "customerName", DisplayName = "Customer Name", DataType = "string", IsRequired = true },
            new() { Name = "amount", DisplayName = "Amount", DataType = "number", IsRequired = true }
        };

        _mockTenantRepository.Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        _mockDocumentTypeRepository.Setup(x => x.GetByIdAsync(documentTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documentType);

        _mockTemplateRepository.Setup(x => x.GetByTenantAndNameAsync(tenantId, name, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentTemplate?)null);

        _mockTemplateRepository.Setup(x => x.AddAsync(It.IsAny<DocumentTemplate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentTemplate t, CancellationToken _) => { t.Id = 1; return t; });

        _mockTemplateRepository.Setup(x => x.GetTemplateWithVariablesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) =>
            {
                var template = new DocumentTemplate
                {
                    Id = id,
                    TenantId = tenantId,
                    DocumentTypeId = documentTypeId,
                    Name = name,
                    Description = description,
                    Content = content,
                    MimeType = mimeType,
                    IsActive = true,
                    CreatedBy = createdBy
                };
                return template;
            });

        // Act
        var result = await _templateService.CreateTemplateAsync(
            tenantId, documentTypeId, name, description, content, mimeType, ".txt",
            variables, null, null, null, null, createdBy);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(name, result.Name);
        Assert.Equal(content, result.Content);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateTemplateAsync_WithInvalidName_ShouldThrowArgumentException(string? invalidName)
    {
        // Arrange & Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _templateService.CreateTemplateAsync(1, 1, invalidName!, "desc", "content", "text/plain",
                null, Enumerable.Empty<TemplateVariable>(), null, null, null, null, "user"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateTemplateAsync_WithInvalidContent_ShouldThrowArgumentException(string? invalidContent)
    {
        // Arrange & Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _templateService.CreateTemplateAsync(1, 1, "name", "desc", invalidContent!, "text/plain",
                null, Enumerable.Empty<TemplateVariable>(), null, null, null, null, "user"));
    }

    [Fact]
    public async Task CreateTemplateAsync_WithNonExistentTenant_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockTenantRepository.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _templateService.CreateTemplateAsync(999, 1, "name", "desc", "content", "text/plain",
                null, Enumerable.Empty<TemplateVariable>(), null, null, null, null, "user"));
    }

    [Fact]
    public async Task CreateTemplateAsync_WithDuplicateName_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var tenantId = 1;
        var name = "Duplicate Template";

        var tenant = new Tenant { Id = tenantId, Name = "Test", Slug = "test", TenantType = "Org" };
        var documentType = new DocumentType { Id = 1, Name = "doc", DisplayName = "Doc" };
        var existingTemplate = new DocumentTemplate { Id = 1, Name = name, TenantId = tenantId };

        _mockTenantRepository.Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        _mockDocumentTypeRepository.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documentType);

        _mockTemplateRepository.Setup(x => x.GetByTenantAndNameAsync(tenantId, name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTemplate);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _templateService.CreateTemplateAsync(tenantId, 1, name, "desc", "content", "text/plain",
                null, Enumerable.Empty<TemplateVariable>(), null, null, null, null, "user"));
    }

    #endregion

    #region GetTemplateByIdAsync Tests

    [Fact]
    public async Task GetTemplateByIdAsync_WithExistingTemplate_ShouldReturnTemplate()
    {
        // Arrange
        var templateId = 1;
        var template = new DocumentTemplate
        {
            Id = templateId,
            Name = "Test Template",
            Content = "Test Content",
            Variables = new List<TemplateVariable>()
        };

        _mockTemplateRepository.Setup(x => x.GetTemplateWithVariablesAsync(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        // Act
        var result = await _templateService.GetTemplateByIdAsync(templateId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(templateId, result.Id);
        Assert.Equal("Test Template", result.Name);
    }

    [Fact]
    public async Task GetTemplateByIdAsync_WithNonExistentTemplate_ShouldReturnNull()
    {
        // Arrange
        _mockTemplateRepository.Setup(x => x.GetTemplateWithVariablesAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentTemplate?)null);

        // Act
        var result = await _templateService.GetTemplateByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region SubstituteVariables Tests

    [Fact]
    public void SubstituteVariables_WithSimpleVariables_ShouldReplaceCorrectly()
    {
        // Arrange
        var content = "Hello {{name}}, your balance is {{amount}}.";
        var variables = new Dictionary<string, string>
        {
            { "name", "John Doe" },
            { "amount", "$1,000.00" }
        };

        // Act
        var result = _templateService.SubstituteVariables(content, variables);

        // Assert
        Assert.Equal("Hello John Doe, your balance is $1,000.00.", result);
    }

    [Fact]
    public void SubstituteVariables_WithMissingVariable_ShouldKeepPlaceholder()
    {
        // Arrange
        var content = "Hello {{name}}, your balance is {{amount}}.";
        var variables = new Dictionary<string, string>
        {
            { "name", "John Doe" }
        };

        // Act
        var result = _templateService.SubstituteVariables(content, variables);

        // Assert
        Assert.Equal("Hello John Doe, your balance is {{amount}}.", result);
    }

    [Fact]
    public void SubstituteVariables_WithEmptyContent_ShouldReturnEmpty()
    {
        // Arrange
        var content = "";
        var variables = new Dictionary<string, string> { { "name", "Test" } };

        // Act
        var result = _templateService.SubstituteVariables(content, variables);

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void SubstituteVariables_WithNoVariables_ShouldReturnOriginal()
    {
        // Arrange
        var content = "No variables here";
        var variables = new Dictionary<string, string>();

        // Act
        var result = _templateService.SubstituteVariables(content, variables);

        // Assert
        Assert.Equal(content, result);
    }

    #endregion

    #region ValidateVariables Tests

    [Fact]
    public void ValidateVariables_WithValidRequiredVariables_ShouldReturnNoErrors()
    {
        // Arrange
        var template = new DocumentTemplate
        {
            Variables = new List<TemplateVariable>
            {
                new() { Name = "name", DisplayName = "Name", IsRequired = true, DataType = "string" },
                new() { Name = "age", DisplayName = "Age", IsRequired = true, DataType = "number" }
            }
        };

        var variables = new Dictionary<string, string>
        {
            { "name", "John Doe" },
            { "age", "30" }
        };

        // Act
        var errors = _templateService.ValidateVariables(template, variables);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateVariables_WithMissingRequiredVariable_ShouldReturnError()
    {
        // Arrange
        var template = new DocumentTemplate
        {
            Variables = new List<TemplateVariable>
            {
                new() { Name = "name", DisplayName = "Name", IsRequired = true, DataType = "string" },
                new() { Name = "email", DisplayName = "Email", IsRequired = true, DataType = "string" }
            }
        };

        var variables = new Dictionary<string, string>
        {
            { "name", "John Doe" }
        };

        // Act
        var errors = _templateService.ValidateVariables(template, variables);

        // Assert
        Assert.Single(errors);
        Assert.Contains("email", errors.Keys);
        Assert.Contains("required", errors["email"], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateVariables_WithInvalidNumber_ShouldReturnError()
    {
        // Arrange
        var template = new DocumentTemplate
        {
            Variables = new List<TemplateVariable>
            {
                new() { Name = "age", DisplayName = "Age", DataType = "number", IsRequired = true }
            }
        };

        var variables = new Dictionary<string, string>
        {
            { "age", "not-a-number" }
        };

        // Act
        var errors = _templateService.ValidateVariables(template, variables);

        // Assert
        Assert.Single(errors);
        Assert.Contains("age", errors.Keys);
        Assert.Contains("number", errors["age"], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateVariables_WithInvalidDate_ShouldReturnError()
    {
        // Arrange
        var template = new DocumentTemplate
        {
            Variables = new List<TemplateVariable>
            {
                new() { Name = "birthdate", DisplayName = "Birth Date", DataType = "date", IsRequired = true }
            }
        };

        var variables = new Dictionary<string, string>
        {
            { "birthdate", "not-a-date" }
        };

        // Act
        var errors = _templateService.ValidateVariables(template, variables);

        // Assert
        Assert.Single(errors);
        Assert.Contains("birthdate", errors.Keys);
        Assert.Contains("date", errors["birthdate"], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateVariables_WithValidationPattern_ShouldValidate()
    {
        // Arrange
        var template = new DocumentTemplate
        {
            Variables = new List<TemplateVariable>
            {
                new()
                {
                    Name = "email",
                    DisplayName = "Email",
                    DataType = "string",
                    ValidationPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                    ValidationMessage = "Invalid email format"
                }
            }
        };

        var invalidVariables = new Dictionary<string, string>
        {
            { "email", "not-an-email" }
        };

        var validVariables = new Dictionary<string, string>
        {
            { "email", "test@example.com" }
        };

        // Act
        var invalidErrors = _templateService.ValidateVariables(template, invalidVariables);
        var validErrors = _templateService.ValidateVariables(template, validVariables);

        // Assert
        Assert.Single(invalidErrors);
        Assert.Contains("Invalid email format", invalidErrors["email"]);
        Assert.Empty(validErrors);
    }

    #endregion

    #region CreateDocumentFromTemplateAsync Tests

    [Fact]
    public async Task CreateDocumentFromTemplateAsync_WithValidVariables_ShouldCreateDocument()
    {
        // Arrange
        var templateId = 1;
        var template = new DocumentTemplate
        {
            Id = templateId,
            TenantId = 1,
            DocumentTypeId = 1,
            Name = "Invoice Template",
            Content = "Invoice for {{customerName}}\nAmount: {{amount}}",
            MimeType = "text/plain",
            FileExtension = ".txt",
            IsActive = true,
            IsDeleted = false,
            Variables = new List<TemplateVariable>
            {
                new() { Name = "customerName", DisplayName = "Customer Name", DataType = "string", IsRequired = true },
                new() { Name = "amount", DisplayName = "Amount", DataType = "number", IsRequired = true }
            }
        };

        var variables = new Dictionary<string, string>
        {
            { "customerName", "John Doe" },
            { "amount", "1000.00" }
        };

        _mockTemplateRepository.Setup(x => x.GetTemplateWithVariablesAsync(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        _mockBlobStorageService.Setup(x => x.UploadAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://storage.blob.core.windows.net/documents/test.txt");

        _mockDocumentRepository.Setup(x => x.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document d, CancellationToken _) => { d.Id = 1; return d; });

        // Act
        var result = await _templateService.CreateDocumentFromTemplateAsync(
            templateId, variables, null, null, null, null, "testuser");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(template.TenantId, result.TenantId);
        Assert.Equal(template.DocumentTypeId, result.DocumentTypeId);
        _mockBlobStorageService.Verify(x => x.UploadAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockDocumentRepository.Verify(x => x.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateDocumentFromTemplateAsync_WithInvalidVariables_ShouldThrowArgumentException()
    {
        // Arrange
        var templateId = 1;
        var template = new DocumentTemplate
        {
            Id = templateId,
            TenantId = 1,
            DocumentTypeId = 1,
            Name = "Test Template",
            Content = "Content with {{name}}",
            IsActive = true,
            IsDeleted = false,
            Variables = new List<TemplateVariable>
            {
                new() { Name = "name", DisplayName = "Name", DataType = "string", IsRequired = true }
            }
        };

        var invalidVariables = new Dictionary<string, string>();

        _mockTemplateRepository.Setup(x => x.GetTemplateWithVariablesAsync(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _templateService.CreateDocumentFromTemplateAsync(
                templateId, invalidVariables, null, null, null, null, "testuser"));
    }

    [Fact]
    public async Task CreateDocumentFromTemplateAsync_WithInactiveTemplate_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var templateId = 1;
        var template = new DocumentTemplate
        {
            Id = templateId,
            IsActive = false,
            IsDeleted = false,
            Variables = new List<TemplateVariable>()
        };

        _mockTemplateRepository.Setup(x => x.GetTemplateWithVariablesAsync(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _templateService.CreateDocumentFromTemplateAsync(
                templateId, new Dictionary<string, string>(), null, null, null, null, "testuser"));
    }

    [Fact]
    public async Task CreateDocumentFromTemplateAsync_WithDeletedTemplate_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var templateId = 1;
        var template = new DocumentTemplate
        {
            Id = templateId,
            IsActive = true,
            IsDeleted = true,
            Variables = new List<TemplateVariable>()
        };

        _mockTemplateRepository.Setup(x => x.GetTemplateWithVariablesAsync(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _templateService.CreateDocumentFromTemplateAsync(
                templateId, new Dictionary<string, string>(), null, null, null, null, "testuser"));
    }

    #endregion

    #region UpdateTemplateAsync Tests

    [Fact]
    public async Task UpdateTemplateAsync_WithValidData_ShouldUpdateTemplate()
    {
        // Arrange
        var templateId = 1;
        var template = new DocumentTemplate
        {
            Id = templateId,
            Name = "Old Name",
            Content = "Old Content",
            IsDeleted = false,
            Variables = new List<TemplateVariable>()
        };

        _mockTemplateRepository.Setup(x => x.GetTemplateWithVariablesAsync(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        // Act
        var result = await _templateService.UpdateTemplateAsync(
            templateId, "New Name", "New Description", "New Content", null, null,
            null, null, null, null, null, true, "testuser");

        // Assert
        Assert.NotNull(result);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTemplateAsync_WithNonExistentTemplate_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockTemplateRepository.Setup(x => x.GetTemplateWithVariablesAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentTemplate?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _templateService.UpdateTemplateAsync(
                999, "Name", null, null, null, null, null, null, null, null, null, null, "testuser"));
    }

    [Fact]
    public async Task UpdateTemplateAsync_WithDeletedTemplate_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var templateId = 1;
        var template = new DocumentTemplate
        {
            Id = templateId,
            Name = "Test",
            IsDeleted = true,
            Variables = new List<TemplateVariable>()
        };

        _mockTemplateRepository.Setup(x => x.GetTemplateWithVariablesAsync(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _templateService.UpdateTemplateAsync(
                templateId, "New Name", null, null, null, null, null, null, null, null, null, null, "testuser"));
    }

    #endregion

    #region DeleteTemplateAsync Tests

    [Fact]
    public async Task DeleteTemplateAsync_WithValidTemplate_ShouldSoftDelete()
    {
        // Arrange
        var templateId = 1;

        // Act
        await _templateService.DeleteTemplateAsync(templateId, "testuser", "No longer needed");

        // Assert
        _mockTemplateRepository.Verify(x => x.SoftDeleteAsync(templateId, "testuser", "No longer needed", It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DeleteTemplateAsync_WithInvalidDeletedBy_ShouldThrowArgumentException(string? invalidDeletedBy)
    {
        // Arrange & Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _templateService.DeleteTemplateAsync(1, invalidDeletedBy!, null));
    }

    #endregion

    #region RestoreTemplateAsync Tests

    [Fact]
    public async Task RestoreTemplateAsync_WithValidTemplate_ShouldRestore()
    {
        // Arrange
        var templateId = 1;

        // Act
        await _templateService.RestoreTemplateAsync(templateId);

        // Assert
        _mockTemplateRepository.Verify(x => x.RestoreAsync(templateId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
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
            new() { Id = 1, TenantId = tenantId, Name = "Template 1" },
            new() { Id = 2, TenantId = tenantId, Name = "Template 2" }
        };

        _mockTemplateRepository.Setup(x => x.GetByTenantIdAsync(tenantId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates);

        // Act
        var result = await _templateService.GetTemplatesByTenantAsync(tenantId, false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    #endregion

    #region GetActiveTemplatesAsync Tests

    [Fact]
    public async Task GetActiveTemplatesAsync_ShouldReturnOnlyActiveTemplates()
    {
        // Arrange
        var tenantId = 1;
        var activeTemplates = new List<DocumentTemplate>
        {
            new() { Id = 1, TenantId = tenantId, Name = "Active Template", IsActive = true }
        };

        _mockTemplateRepository.Setup(x => x.GetActiveTemplatesAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeTemplates);

        // Act
        var result = await _templateService.GetActiveTemplatesAsync(tenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.All(result, t => Assert.True(t.IsActive));
    }

    #endregion
}
