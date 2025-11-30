using Moq;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Interfaces;
using TaskMaster.DocumentService.Core.Services;

namespace TaskMaster.DocumentService.Core.Tests.Services;

/// <summary>
/// Unit tests for the TemplateService class.
/// </summary>
public class TemplateServiceTests
{
    private readonly Mock<ITemplateRepository> _mockRepository;
    private readonly TemplateService _service;

    public TemplateServiceTests()
    {
        _mockRepository = new Mock<ITemplateRepository>();
        _service = new TemplateService(_mockRepository.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TemplateService(null!));
    }

    #endregion

    #region GetTemplateAsync Tests

    [Fact]
    public async Task GetTemplateAsync_WithValidId_ReturnsTemplate()
    {
        // Arrange
        var template = new DocumentTemplate
        {
            Id = 1,
            TenantId = 100,
            Name = "Test Template",
            Content = "Hello {{name}}",
            IsPublic = false
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        // Act
        var result = await _service.GetTemplateAsync(1, 100, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Test Template", result.Name);
    }

    [Fact]
    public async Task GetTemplateAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentTemplate?)null);

        // Act
        var result = await _service.GetTemplateAsync(999, 100, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetTemplateAsync_WithUnauthorizedTenant_ReturnsNull()
    {
        // Arrange
        var template = new DocumentTemplate
        {
            Id = 1,
            TenantId = 100,
            Name = "Test Template",
            IsPublic = false
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        // Act - Try to access with different tenant ID
        var result = await _service.GetTemplateAsync(1, 200, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetTemplateAsync_WithPublicTemplate_AllowsAccessFromAnyTenant()
    {
        // Arrange
        var template = new DocumentTemplate
        {
            Id = 1,
            TenantId = 100,
            Name = "Public Template",
            IsPublic = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        // Act - Access from different tenant
        var result = await _service.GetTemplateAsync(1, 200, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    #endregion

    #region CreateTemplateAsync Tests

    [Fact]
    public async Task CreateTemplateAsync_WithValidTemplate_SetsDefaultValues()
    {
        // Arrange
        var template = new DocumentTemplate
        {
            Name = "New Template",
            Content = "Content"
        };

        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<DocumentTemplate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentTemplate t, CancellationToken ct) => t);

        // Act
        var result = await _service.CreateTemplateAsync(template, "testuser", CancellationToken.None);

        // Assert
        Assert.Equal("testuser", result.CreatedBy);
        Assert.Equal(1, result.Version);
        Assert.True(result.IsCurrentVersion);
        Assert.True(result.IsActive);
        Assert.False(result.IsDeleted);
    }

    #endregion

    #region UpdateTemplateAsync Tests

    [Fact]
    public async Task UpdateTemplateAsync_WithValidTemplate_SetsUpdatedByAndTimestamp()
    {
        // Arrange
        var template = new DocumentTemplate
        {
            Id = 1,
            Name = "Updated Template"
        };

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<DocumentTemplate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentTemplate t, CancellationToken ct) => t);

        // Act
        var result = await _service.UpdateTemplateAsync(template, "updateuser", CancellationToken.None);

        // Assert
        Assert.Equal("updateuser", result.UpdatedBy);
        Assert.NotNull(result.UpdatedAt);
    }

    #endregion

    #region DeleteTemplateAsync Tests

    [Fact]
    public async Task DeleteTemplateAsync_WithValidId_ReturnsTrue()
    {
        // Arrange
        var template = new DocumentTemplate
        {
            Id = 1,
            TenantId = 100,
            Name = "Template to Delete"
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _mockRepository.Setup(r => r.DeleteAsync(1, "deleteuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.DeleteTemplateAsync(1, 100, "deleteuser", CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteTemplateAsync_WithUnauthorizedTenant_ReturnsFalse()
    {
        // Arrange
        var template = new DocumentTemplate
        {
            Id = 1,
            TenantId = 100,
            Name = "Template"
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        // Act - Try to delete with different tenant ID
        var result = await _service.DeleteTemplateAsync(1, 200, "deleteuser", CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region RenderTemplateAsync Tests

    [Fact]
    public async Task RenderTemplateAsync_WithAllVariables_SuccessfullySubstitutes()
    {
        // Arrange
        var template = new DocumentTemplate
        {
            Id = 1,
            TenantId = 100,
            Name = "Test Template",
            Content = "Hello {{name}}, welcome to {{company}}!",
            Variables = "[{\"Name\":\"name\",\"Label\":\"Name\",\"Type\":\"Text\",\"IsRequired\":true},{\"Name\":\"company\",\"Label\":\"Company\",\"Type\":\"Text\",\"IsRequired\":true}]"
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _mockRepository.Setup(r => r.LogUsageAsync(It.IsAny<TemplateUsageLog>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TemplateUsageLog log, CancellationToken ct) => log);

        var variables = new Dictionary<string, string>
        {
            { "name", "John Doe" },
            { "company", "Acme Corp" }
        };

        // Act
        var result = await _service.RenderTemplateAsync(1, 100, variables, "testuser", null, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Hello John Doe, welcome to Acme Corp!", result.RenderedContent);
        Assert.Equal(2, result.SubstitutedVariables.Count);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task RenderTemplateAsync_WithMissingRequiredVariable_ReturnsErrors()
    {
        // Arrange
        var template = new DocumentTemplate
        {
            Id = 1,
            TenantId = 100,
            Content = "Hello {{name}}",
            Variables = "[{\"Name\":\"name\",\"Label\":\"Name\",\"Type\":\"Text\",\"IsRequired\":true}]"
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _mockRepository.Setup(r => r.LogUsageAsync(It.IsAny<TemplateUsageLog>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TemplateUsageLog log, CancellationToken ct) => log);

        var variables = new Dictionary<string, string>();

        // Act
        var result = await _service.RenderTemplateAsync(1, 100, variables, "testuser", null, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Required variable 'name' is missing", result.Errors);
        Assert.Contains("name", result.MissingVariables);
    }

    [Fact]
    public async Task RenderTemplateAsync_WithDefaultValue_UsesDefault()
    {
        // Arrange
        var template = new DocumentTemplate
        {
            Id = 1,
            TenantId = 100,
            Content = "Hello {{name}}",
            Variables = "[{\"Name\":\"name\",\"Label\":\"Name\",\"Type\":\"Text\",\"DefaultValue\":\"Guest\",\"IsRequired\":false}]"
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _mockRepository.Setup(r => r.LogUsageAsync(It.IsAny<TemplateUsageLog>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TemplateUsageLog log, CancellationToken ct) => log);

        var variables = new Dictionary<string, string>();

        // Act
        var result = await _service.RenderTemplateAsync(1, 100, variables, "testuser", null, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Hello Guest", result.RenderedContent);
        Assert.Contains("Using default value for variable 'name'", result.Warnings);
    }

    [Fact]
    public async Task RenderTemplateAsync_WithNonExistentTemplate_ReturnsError()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentTemplate?)null);

        var variables = new Dictionary<string, string>();

        // Act
        var result = await _service.RenderTemplateAsync(999, 100, variables, "testuser", null, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Template not found or access denied", result.Errors);
    }

    [Fact]
    public async Task RenderTemplateAsync_LogsUsage()
    {
        // Arrange
        var template = new DocumentTemplate
        {
            Id = 1,
            TenantId = 100,
            Content = "Test {{var}}",
            Variables = "[{\"Name\":\"var\",\"Label\":\"Var\",\"Type\":\"Text\",\"IsRequired\":false,\"DefaultValue\":\"value\"}]"
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _mockRepository.Setup(r => r.LogUsageAsync(It.IsAny<TemplateUsageLog>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TemplateUsageLog log, CancellationToken ct) => log);

        var variables = new Dictionary<string, string>();

        // Act
        await _service.RenderTemplateAsync(1, 100, variables, "testuser", 12345, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.LogUsageAsync(
            It.Is<TemplateUsageLog>(log =>
                log.TemplateId == 1 &&
                log.TenantId == 100 &&
                log.UsedBy == "testuser" &&
                log.DocumentId == 12345),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region ParseTemplateVariables Tests

    [Fact]
    public void ParseTemplateVariables_WithValidJson_ReturnsVariables()
    {
        // Arrange
        var json = "[{\"Name\":\"var1\",\"Label\":\"Variable 1\",\"Type\":\"Text\"}]";

        // Act
        var result = _service.ParseTemplateVariables(json);

        // Assert
        Assert.Single(result);
        Assert.Equal("var1", result[0].Name);
        Assert.Equal("Variable 1", result[0].Label);
    }

    [Fact]
    public void ParseTemplateVariables_WithEmptyString_ReturnsEmptyList()
    {
        // Act
        var result = _service.ParseTemplateVariables("");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ParseTemplateVariables_WithNull_ReturnsEmptyList()
    {
        // Act
        var result = _service.ParseTemplateVariables(null);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ParseTemplateVariables_WithInvalidJson_ReturnsEmptyList()
    {
        // Arrange
        var invalidJson = "not valid json";

        // Act
        var result = _service.ParseTemplateVariables(invalidJson);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region ValidateTemplate Tests

    [Fact]
    public void ValidateTemplate_WithEmptyContent_ReturnsError()
    {
        // Arrange
        var variables = new List<Models.TemplateVariable>();

        // Act
        var errors = _service.ValidateTemplate("", variables);

        // Assert
        Assert.Contains("Template content cannot be empty", errors);
    }

    [Fact]
    public void ValidateTemplate_WithUndefinedVariable_ReturnsError()
    {
        // Arrange
        var content = "Hello {{undefinedVar}}";
        var variables = new List<Models.TemplateVariable>();

        // Act
        var errors = _service.ValidateTemplate(content, variables);

        // Assert
        Assert.Contains("Variable 'undefinedVar' used in content but not defined in variables", errors);
    }

    [Fact]
    public void ValidateTemplate_WithDuplicateVariableNames_ReturnsError()
    {
        // Arrange
        var content = "Hello {{name}}";
        var variables = new List<Models.TemplateVariable>
        {
            new() { Name = "name", Label = "Name 1" },
            new() { Name = "name", Label = "Name 2" }
        };

        // Act
        var errors = _service.ValidateTemplate(content, variables);

        // Assert
        Assert.Contains("Duplicate variable name: 'name'", errors);
    }

    [Fact]
    public void ValidateTemplate_WithEmptyVariableName_ReturnsError()
    {
        // Arrange
        var content = "Hello";
        var variables = new List<Models.TemplateVariable>
        {
            new() { Name = "", Label = "Label" }
        };

        // Act
        var errors = _service.ValidateTemplate(content, variables);

        // Assert
        Assert.Contains("Variable name cannot be empty", errors);
    }

    [Fact]
    public void ValidateTemplate_WithEmptyVariableLabel_ReturnsError()
    {
        // Arrange
        var content = "Hello {{name}}";
        var variables = new List<Models.TemplateVariable>
        {
            new() { Name = "name", Label = "" }
        };

        // Act
        var errors = _service.ValidateTemplate(content, variables);

        // Assert
        Assert.Contains("Variable 'name' must have a label", errors);
    }

    [Fact]
    public void ValidateTemplate_WithInvalidRegexPattern_ReturnsError()
    {
        // Arrange
        var content = "Hello {{name}}";
        var variables = new List<Models.TemplateVariable>
        {
            new() { Name = "name", Label = "Name", ValidationPattern = "[invalid(" }
        };

        // Act
        var errors = _service.ValidateTemplate(content, variables);

        // Assert
        Assert.Contains("Variable 'name' has invalid validation pattern", errors);
    }

    [Fact]
    public void ValidateTemplate_WithValidTemplate_ReturnsNoErrors()
    {
        // Arrange
        var content = "Hello {{name}}, welcome to {{company}}!";
        var variables = new List<Models.TemplateVariable>
        {
            new() { Name = "name", Label = "Name", Type = "Text" },
            new() { Name = "company", Label = "Company", Type = "Text" }
        };

        // Act
        var errors = _service.ValidateTemplate(content, variables);

        // Assert
        Assert.Empty(errors);
    }

    #endregion

    #region GetTemplatesAsync Tests

    [Fact]
    public async Task GetTemplatesAsync_CallsRepositoryWithCorrectParameters()
    {
        // Arrange
        var templates = new List<DocumentTemplate>
        {
            new() { Id = 1, Name = "Template 1" },
            new() { Id = 2, Name = "Template 2" }
        };

        _mockRepository.Setup(r => r.GetByTenantIdAsync(100, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates);

        // Act
        var result = await _service.GetTemplatesAsync(100, true, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        _mockRepository.Verify(r => r.GetByTenantIdAsync(100, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetTemplatesByTypeAsync Tests

    [Fact]
    public async Task GetTemplatesByTypeAsync_CallsRepositoryWithCorrectType()
    {
        // Arrange
        var templates = new List<DocumentTemplate>
        {
            new() { Id = 1, Name = "Email Template", TemplateType = "Email" }
        };

        _mockRepository.Setup(r => r.GetByTypeAsync(100, "Email", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates);

        // Act
        var result = await _service.GetTemplatesByTypeAsync(100, "Email", CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("Email", result[0].TemplateType);
    }

    #endregion

    #region SearchTemplatesAsync Tests

    [Fact]
    public async Task SearchTemplatesAsync_CallsRepositoryWithSearchTerm()
    {
        // Arrange
        var templates = new List<DocumentTemplate>
        {
            new() { Id = 1, Name = "Invoice Template" }
        };

        _mockRepository.Setup(r => r.SearchByNameAsync(100, "Invoice", It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates);

        // Act
        var result = await _service.SearchTemplatesAsync(100, "Invoice", CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Contains("Invoice", result[0].Name);
    }

    #endregion
}
