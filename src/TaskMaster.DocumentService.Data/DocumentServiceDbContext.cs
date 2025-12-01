using Microsoft.EntityFrameworkCore;
using TaskMaster.DocumentService.Core.Entities;

namespace TaskMaster.DocumentService.Data;

/// <summary>
/// Database context for the Document Service.
/// </summary>
public class DocumentServiceDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentServiceDbContext"/> class.
    /// </summary>
    /// <param name="options">The database context options.</param>
    public DocumentServiceDbContext(DbContextOptions<DocumentServiceDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the Tenants DbSet.
    /// </summary>
    public DbSet<Tenant> Tenants => Set<Tenant>();

    /// <summary>
    /// Gets or sets the DocumentTypes DbSet.
    /// </summary>
    public DbSet<DocumentType> DocumentTypes => Set<DocumentType>();

    /// <summary>
    /// Gets or sets the Documents DbSet.
    /// </summary>
    public DbSet<Document> Documents => Set<Document>();

    /// <summary>
    /// Gets or sets the Collections DbSet.
    /// </summary>
    public DbSet<Collection> Collections => Set<Collection>();

    /// <summary>
    /// Gets or sets the CollectionDocuments DbSet.
    /// </summary>
    public DbSet<CollectionDocument> CollectionDocuments => Set<CollectionDocument>();

    /// <summary>
    /// Gets or sets the CodeReviews DbSet.
    /// </summary>
    public DbSet<CodeReview> CodeReviews => Set<CodeReview>();

    /// <summary>
    /// Configures the entity models and relationships.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure schema
        modelBuilder.HasDefaultSchema("docs");

        // Configure Tenant entity
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("Tenants");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.TenantType)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Slug)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasIndex(e => e.Slug)
                .IsUnique()
                .HasDatabaseName("IX_Tenants_Slug");

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            // Self-referencing relationship for hierarchical tenants
            entity.HasOne(e => e.ParentTenant)
                .WithMany(e => e.ChildTenants)
                .HasForeignKey(e => e.ParentTenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure DocumentType entity
        modelBuilder.Entity<DocumentType>(entity =>
        {
            entity.ToTable("DocumentTypes");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasIndex(e => e.Name)
                .IsUnique()
                .HasDatabaseName("IX_DocumentTypes_Name");

            entity.Property(e => e.DisplayName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Description)
                .HasMaxLength(1000);

            entity.Property(e => e.DefaultTags)
                .HasMaxLength(500);

            entity.Property(e => e.Icon)
                .HasMaxLength(50);

            entity.Property(e => e.ExtensionTableName)
                .HasMaxLength(100);

            entity.Property(e => e.IsContentIndexed)
                .IsRequired()
                .HasDefaultValue(true);

            entity.Property(e => e.HasExtensionTable)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);
        });

        // Configure Document entity
        modelBuilder.Entity<Document>(entity =>
        {
            entity.ToTable("Documents");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.Description)
                .HasMaxLength(2000);

            entity.Property(e => e.BlobPath)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.ContentHash)
                .HasMaxLength(64);

            entity.Property(e => e.MimeType)
                .HasMaxLength(100);

            entity.Property(e => e.OriginalFileName)
                .HasMaxLength(500);

            entity.Property(e => e.MeilisearchId)
                .HasMaxLength(100);

            entity.Property(e => e.Version)
                .IsRequired()
                .HasDefaultValue(1);

            entity.Property(e => e.IsCurrentVersion)
                .IsRequired()
                .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.CreatedBy)
                .HasMaxLength(200);

            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(200);

            entity.Property(e => e.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            entity.HasIndex(e => e.IsDeleted)
                .HasDatabaseName("IX_Documents_IsDeleted");

            entity.Property(e => e.DeletedBy)
                .HasMaxLength(200);

            entity.Property(e => e.DeletedReason)
                .HasMaxLength(500);

            entity.Property(e => e.IsArchived)
                .IsRequired()
                .HasDefaultValue(false);

            // Foreign key relationships
            entity.HasOne(e => e.Tenant)
                .WithMany(e => e.Documents)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.TenantId)
                .HasDatabaseName("IX_Documents_TenantId");

            entity.HasOne(e => e.DocumentType)
                .WithMany(e => e.Documents)
                .HasForeignKey(e => e.DocumentTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Self-referencing relationship for versioning
            entity.HasOne(e => e.ParentDocument)
                .WithMany(e => e.ChildDocuments)
                .HasForeignKey(e => e.ParentDocumentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Collection entity
        modelBuilder.Entity<Collection>(entity =>
        {
            entity.ToTable("Collections");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Description)
                .HasMaxLength(2000);

            entity.Property(e => e.Slug)
                .IsRequired()
                .HasMaxLength(200);

            entity.HasIndex(e => new { e.Slug, e.TenantId })
                .IsUnique()
                .HasDatabaseName("IX_Collections_Slug_TenantId");

            entity.Property(e => e.IsPublished)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(e => e.PublishedBy)
                .HasMaxLength(200);

            entity.Property(e => e.SortOrder)
                .IsRequired()
                .HasDefaultValue(0);

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.CreatedBy)
                .HasMaxLength(200);

            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(200);

            entity.Property(e => e.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            entity.HasIndex(e => e.IsDeleted)
                .HasDatabaseName("IX_Collections_IsDeleted");

            entity.Property(e => e.DeletedBy)
                .HasMaxLength(200);

            entity.Property(e => e.DeletedReason)
                .HasMaxLength(500);

            // Foreign key relationship with Tenant
            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.TenantId)
                .HasDatabaseName("IX_Collections_TenantId");
        });

        // Configure CollectionDocument entity (junction table)
        modelBuilder.Entity<CollectionDocument>(entity =>
        {
            entity.ToTable("CollectionDocuments");
            entity.HasKey(e => new { e.CollectionId, e.DocumentId });

            entity.Property(e => e.SortOrder)
                .IsRequired()
                .HasDefaultValue(0);

            entity.Property(e => e.AddedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.AddedBy)
                .HasMaxLength(200);

            // Foreign key relationships
            entity.HasOne(e => e.Collection)
                .WithMany(e => e.CollectionDocuments)
                .HasForeignKey(e => e.CollectionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Document)
                .WithMany()
                .HasForeignKey(e => e.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.CollectionId)
                .HasDatabaseName("IX_CollectionDocuments_CollectionId");

            entity.HasIndex(e => e.DocumentId)
                .HasDatabaseName("IX_CollectionDocuments_DocumentId");
        });

        // Configure CodeReview entity (extension table for Document)
        modelBuilder.Entity<CodeReview>(entity =>
        {
            entity.ToTable("CodeReviews");
            entity.HasKey(e => e.DocumentId);

            entity.Property(e => e.PullRequestNumber);

            entity.Property(e => e.RepositoryName)
                .HasMaxLength(200);

            entity.Property(e => e.BranchName)
                .HasMaxLength(200);

            entity.Property(e => e.BaseBranchName)
                .HasMaxLength(200);

            entity.Property(e => e.Author)
                .HasMaxLength(200);

            entity.Property(e => e.Reviewers)
                .HasMaxLength(2000);

            entity.Property(e => e.Status)
                .HasMaxLength(50);

            entity.Property(e => e.PullRequestUrl)
                .HasMaxLength(500);

            entity.Property(e => e.CommitSha)
                .HasMaxLength(64);

            entity.Property(e => e.ApprovedBy)
                .HasMaxLength(200);

            entity.Property(e => e.MergedBy)
                .HasMaxLength(200);

            entity.Property(e => e.SourceBlobPath)
                .HasMaxLength(500);

            entity.Property(e => e.MigratedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.MigrationBatchId)
                .HasMaxLength(100);

            // Foreign key relationship with Document
            entity.HasOne(e => e.Document)
                .WithOne()
                .HasForeignKey<CodeReview>(e => e.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.PullRequestNumber)
                .HasDatabaseName("IX_CodeReviews_PullRequestNumber");

            entity.HasIndex(e => e.Status)
                .HasDatabaseName("IX_CodeReviews_Status");

            entity.HasIndex(e => e.MigrationBatchId)
                .HasDatabaseName("IX_CodeReviews_MigrationBatchId");
        });
    }
}
