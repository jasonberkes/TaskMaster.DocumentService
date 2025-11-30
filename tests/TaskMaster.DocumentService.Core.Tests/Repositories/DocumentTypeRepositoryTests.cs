using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Tests.Helpers;
using TaskMaster.DocumentService.Data.Repositories;

namespace TaskMaster.DocumentService.Core.Tests.Repositories;

/// <summary>
/// Unit tests for DocumentTypeRepository.
/// </summary>
public class DocumentTypeRepositoryTests : IDisposable
{
    private readonly DocumentTypeRepository _repository;
    private readonly Data.DocumentServiceDbContext _context;

    public DocumentTypeRepositoryTests()
    {
        _context = TestDbContextFactory.Create();
        _repository = new DocumentTypeRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsDocumentType()
    {
        // Arrange
        var docType = new DocumentType
        {
            Name = "Invoice",
            DisplayName = "Invoice Document",
            IsContentIndexed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.DocumentTypes.AddAsync(docType);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(docType.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(docType.Id, result.Id);
        Assert.Equal("Invoice", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange & Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByNameAsync_WithValidName_ReturnsDocumentType()
    {
        // Arrange
        var docType = new DocumentType
        {
            Name = "Contract",
            DisplayName = "Contract Document",
            IsContentIndexed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.DocumentTypes.AddAsync(docType);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByNameAsync("Contract");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Contract", result.Name);
    }

    [Fact]
    public async Task GetByNameAsync_WithInvalidName_ReturnsNull()
    {
        // Arrange & Act
        var result = await _repository.GetByNameAsync("NonExistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetActiveDocumentTypesAsync_ReturnsOnlyActiveTypes()
    {
        // Arrange
        var activeType = new DocumentType
        {
            Name = "ActiveType",
            DisplayName = "Active Type",
            IsContentIndexed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var inactiveType = new DocumentType
        {
            Name = "InactiveType",
            DisplayName = "Inactive Type",
            IsContentIndexed = true,
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };
        await _context.DocumentTypes.AddRangeAsync(activeType, inactiveType);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveDocumentTypesAsync();

        // Assert
        Assert.Single(result);
        Assert.Contains(result, t => t.Name == "ActiveType");
        Assert.DoesNotContain(result, t => t.Name == "InactiveType");
    }

    [Fact]
    public async Task GetIndexableTypesAsync_ReturnsOnlyIndexableActiveTypes()
    {
        // Arrange
        var indexableType = new DocumentType
        {
            Name = "IndexableType",
            DisplayName = "Indexable Type",
            IsContentIndexed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var nonIndexableType = new DocumentType
        {
            Name = "NonIndexableType",
            DisplayName = "Non-Indexable Type",
            IsContentIndexed = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var inactiveIndexableType = new DocumentType
        {
            Name = "InactiveIndexableType",
            DisplayName = "Inactive Indexable Type",
            IsContentIndexed = true,
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };
        await _context.DocumentTypes.AddRangeAsync(indexableType, nonIndexableType, inactiveIndexableType);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetIndexableTypesAsync();

        // Assert
        Assert.Single(result);
        Assert.Contains(result, t => t.Name == "IndexableType");
    }

    [Fact]
    public async Task GetTypesWithExtensionTablesAsync_ReturnsTypesWithExtensions()
    {
        // Arrange
        var withExtension = new DocumentType
        {
            Name = "WithExtension",
            DisplayName = "With Extension",
            HasExtensionTable = true,
            ExtensionTableName = "CustomExtensions",
            IsContentIndexed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var withoutExtension = new DocumentType
        {
            Name = "WithoutExtension",
            DisplayName = "Without Extension",
            HasExtensionTable = false,
            IsContentIndexed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.DocumentTypes.AddRangeAsync(withExtension, withoutExtension);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetTypesWithExtensionTablesAsync();

        // Assert
        Assert.Single(result);
        Assert.Contains(result, t => t.Name == "WithExtension");
    }

    [Fact]
    public async Task AddAsync_AddsDocumentTypeToDatabase()
    {
        // Arrange
        var docType = new DocumentType
        {
            Name = "NewType",
            DisplayName = "New Type",
            Description = "A new document type",
            IsContentIndexed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await _repository.AddAsync(docType);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _repository.GetByNameAsync("NewType");
        Assert.NotNull(result);
        Assert.Equal("New Type", result.DisplayName);
    }

    [Fact]
    public async Task Update_UpdatesDocumentTypeInDatabase()
    {
        // Arrange
        var docType = new DocumentType
        {
            Name = "Original",
            DisplayName = "Original Display",
            IsContentIndexed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.DocumentTypes.AddAsync(docType);
        await _context.SaveChangesAsync();

        // Act
        docType.DisplayName = "Updated Display";
        docType.Description = "Updated description";
        _repository.Update(docType);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _repository.GetByIdAsync(docType.Id);
        Assert.NotNull(result);
        Assert.Equal("Updated Display", result.DisplayName);
        Assert.Equal("Updated description", result.Description);
    }

    [Fact]
    public async Task Remove_DeletesDocumentTypeFromDatabase()
    {
        // Arrange
        var docType = new DocumentType
        {
            Name = "ToDelete",
            DisplayName = "To Delete",
            IsContentIndexed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.DocumentTypes.AddAsync(docType);
        await _context.SaveChangesAsync();
        var docTypeId = docType.Id;

        // Act
        _repository.Remove(docType);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _repository.GetByIdAsync(docTypeId);
        Assert.Null(result);
    }

    [Fact]
    public async Task CountAsync_ReturnsCorrectCount()
    {
        // Arrange
        var types = new[]
        {
            new DocumentType { Name = "Type1", DisplayName = "Type 1", IsContentIndexed = true, IsActive = true, CreatedAt = DateTime.UtcNow },
            new DocumentType { Name = "Type2", DisplayName = "Type 2", IsContentIndexed = false, IsActive = true, CreatedAt = DateTime.UtcNow },
            new DocumentType { Name = "Type3", DisplayName = "Type 3", IsContentIndexed = true, IsActive = true, CreatedAt = DateTime.UtcNow }
        };
        await _context.DocumentTypes.AddRangeAsync(types);
        await _context.SaveChangesAsync();

        // Act
        var totalCount = await _repository.CountAsync();
        var indexableCount = await _repository.CountAsync(t => t.IsContentIndexed);

        // Assert
        Assert.Equal(3, totalCount);
        Assert.Equal(2, indexableCount);
    }

    [Fact]
    public async Task AnyAsync_ReturnsCorrectResult()
    {
        // Arrange
        var docType = new DocumentType
        {
            Name = "TestType",
            DisplayName = "Test Type",
            IsContentIndexed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.DocumentTypes.AddAsync(docType);
        await _context.SaveChangesAsync();

        // Act
        var exists = await _repository.AnyAsync(t => t.Name == "TestType");
        var notExists = await _repository.AnyAsync(t => t.Name == "NonExistent");

        // Assert
        Assert.True(exists);
        Assert.False(notExists);
    }
}
