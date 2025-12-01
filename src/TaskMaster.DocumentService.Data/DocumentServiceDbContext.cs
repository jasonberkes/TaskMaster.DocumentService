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
    /// Gets or sets the DocumentTemplates DbSet.
    /// </summary>
    public DbSet<DocumentTemplate> DocumentTemplates => Set<DocumentTemplate>();

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

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Slug)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Description)
                .HasMaxLength(1000);

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.CreatedBy)
                .HasMaxLength(200);

            entity.Property(e => e.ModifiedBy)
                .HasMaxLength(200);

            entity.HasIndex(e => e.Slug)
                .IsUnique()
                .HasDatabaseName("IX_Tenants_Slug");
        });

        // Configure DocumentType entity
        modelBuilder.Entity<DocumentType>(entity =>
        {
            entity.ToTable("DocumentTypes");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Description)
                .HasMaxLength(1000);

            entity.Property(e => e.MimeTypes)
                .HasMaxLength(500);

            entity.Property(e => e.FileExtensions)
                .HasMaxLength(200);

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.CreatedBy)
                .HasMaxLength(200);

            entity.Property(e => e.ModifiedBy)
                .HasMaxLength(200);

            entity.HasIndex(e => e.Name)
                .IsUnique()
                .HasDatabaseName("IX_DocumentTypes_Name");
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

            entity.Property(e => e.FileName)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.BlobPath)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(e => e.ContentHash)
                .HasMaxLength(64);

            entity.Property(e => e.MimeType)
                .HasMaxLength(200);

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.CreatedBy)
                .HasMaxLength(200);

            entity.Property(e => e.ModifiedBy)
                .HasMaxLength(200);

            entity.Property(e => e.DeletedBy)
                .HasMaxLength(200);

            entity.Property(e => e.DeletedReason)
                .HasMaxLength(500);

            // Foreign key relationships
            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.DocumentType)
                .WithMany()
                .HasForeignKey(e => e.DocumentTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.TenantId)
                .HasDatabaseName("IX_Documents_TenantId");

            entity.HasIndex(e => e.DocumentTypeId)
                .HasDatabaseName("IX_Documents_DocumentTypeId");

            entity.HasIndex(e => e.ContentHash)
                .HasDatabaseName("IX_Documents_ContentHash");
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

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.CreatedBy)
                .HasMaxLength(200);

            entity.Property(e => e.ModifiedBy)
                .HasMaxLength(200);

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

        // Configure DocumentTemplate entity
        modelBuilder.Entity<DocumentTemplate>(entity =>
        {
            entity.ToTable("DocumentTemplates");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Description)
                .HasMaxLength(2000);

            entity.Property(e => e.Category)
                .HasMaxLength(100);

            entity.Property(e => e.TemplateContent)
                .IsRequired();

            entity.Property(e => e.Version)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.CreatedBy)
                .HasMaxLength(200);

            entity.Property(e => e.ModifiedBy)
                .HasMaxLength(200);

            entity.Property(e => e.DeletedBy)
                .HasMaxLength(200);

            // Foreign key relationships
            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.TenantId)
                .HasDatabaseName("IX_DocumentTemplates_TenantId");

            entity.HasOne(e => e.DocumentType)
                .WithMany()
                .HasForeignKey(e => e.DocumentTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.TenantId, e.Name })
                .HasDatabaseName("IX_DocumentTemplates_TenantId_Name");

            entity.HasIndex(e => new { e.TenantId, e.Category })
                .HasDatabaseName("IX_DocumentTemplates_TenantId_Category");

            entity.HasIndex(e => e.IsActive)
                .HasDatabaseName("IX_DocumentTemplates_IsActive");

            // Self-referencing relationship for versioning
            entity.HasOne(e => e.ParentTemplate)
                .WithMany(e => e.ChildTemplates)
                .HasForeignKey(e => e.ParentTemplateId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
