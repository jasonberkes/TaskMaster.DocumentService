using FluentAssertions;
using TaskMaster.DocumentService.Core.Entities;

namespace TaskMaster.DocumentService.Core.Tests.Entities;

/// <summary>
/// Unit tests for Document entity.
/// </summary>
public class DocumentTests
{
    [Fact]
    public void Document_DefaultValues_ShouldBeSetCorrectly()
    {
        // Act
        var document = new Document();

        // Assert
        document.Title.Should().Be(string.Empty);
        document.BlobPath.Should().Be(string.Empty);
        document.Version.Should().Be(1);
        document.IsCurrentVersion.Should().BeTrue();
        document.IsDeleted.Should().BeFalse();
        document.IsArchived.Should().BeFalse();
        document.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        document.ChildVersions.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Document_SetProperties_ShouldWork()
    {
        // Arrange
        var tenant = new Tenant { Id = 1, Name = "Test Tenant" };
        var docType = new DocumentType { Id = 1, Name = "TestDoc" };

        // Act
        var document = new Document
        {
            Id = 100,
            TenantId = 1,
            DocumentTypeId = 1,
            Title = "Test Document",
            Description = "Test description",
            BlobPath = "tenant1/2025/01/01/test.pdf",
            ContentHash = "abc123",
            FileSizeBytes = 1024,
            MimeType = "application/pdf",
            OriginalFileName = "test.pdf",
            ExtractedText = "Extracted content",
            Version = 2,
            IsCurrentVersion = true,
            CreatedBy = "TestUser",
            Tenant = tenant,
            DocumentType = docType
        };

        // Assert
        document.Id.Should().Be(100);
        document.TenantId.Should().Be(1);
        document.DocumentTypeId.Should().Be(1);
        document.Title.Should().Be("Test Document");
        document.Description.Should().Be("Test description");
        document.BlobPath.Should().Be("tenant1/2025/01/01/test.pdf");
        document.ContentHash.Should().Be("abc123");
        document.FileSizeBytes.Should().Be(1024);
        document.MimeType.Should().Be("application/pdf");
        document.OriginalFileName.Should().Be("test.pdf");
        document.ExtractedText.Should().Be("Extracted content");
        document.Version.Should().Be(2);
        document.IsCurrentVersion.Should().BeTrue();
        document.CreatedBy.Should().Be("TestUser");
        document.Tenant.Should().Be(tenant);
        document.DocumentType.Should().Be(docType);
    }

    [Fact]
    public void Document_SoftDelete_ShouldSetDeletedProperties()
    {
        // Arrange
        var document = new Document
        {
            Title = "Test Document",
            BlobPath = "test/path"
        };

        // Act
        document.IsDeleted = true;
        document.DeletedAt = DateTime.UtcNow;
        document.DeletedBy = "AdminUser";
        document.DeletedReason = "Test deletion";

        // Assert
        document.IsDeleted.Should().BeTrue();
        document.DeletedAt.Should().NotBeNull();
        document.DeletedBy.Should().Be("AdminUser");
        document.DeletedReason.Should().Be("Test deletion");
    }

    [Fact]
    public void Document_Versioning_ShouldSupportParentChildRelationship()
    {
        // Arrange
        var parentDocument = new Document
        {
            Id = 1,
            Title = "Document v1",
            BlobPath = "path/v1",
            Version = 1,
            IsCurrentVersion = false
        };

        var childDocument = new Document
        {
            Id = 2,
            Title = "Document v2",
            BlobPath = "path/v2",
            Version = 2,
            IsCurrentVersion = true,
            ParentDocumentId = 1,
            ParentDocument = parentDocument
        };

        // Act
        parentDocument.ChildVersions.Add(childDocument);

        // Assert
        childDocument.ParentDocumentId.Should().Be(1);
        childDocument.ParentDocument.Should().Be(parentDocument);
        parentDocument.ChildVersions.Should().ContainSingle().Which.Should().Be(childDocument);
    }
}
