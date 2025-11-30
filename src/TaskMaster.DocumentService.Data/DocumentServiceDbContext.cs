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
    /// Gets or sets the Documents DbSet.
    /// </summary>
    public DbSet<Document> Documents { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Tenants DbSet.
    /// </summary>
    public DbSet<Tenant> Tenants { get; set; } = null!;

    /// <summary>
    /// Gets or sets the DocumentTypes DbSet.
    /// </summary>
    public DbSet<DocumentType> DocumentTypes { get; set; } = null!;

    /// <summary>
    /// Configures the entity models and their relationships.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);

            // Self-referencing relationship for hierarchical tenancy
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

            entity.HasIndex(e => e.Name)
                .IsUnique()
                .HasDatabaseName("IX_DocumentTypes_Name");

            entity.Property(e => e.IsContentIndexed)
                .HasDefaultValue(true);

            entity.Property(e => e.HasExtensionTable)
                .HasDefaultValue(false);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.IsActive)
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
                .HasDefaultValue(1);

            entity.Property(e => e.IsCurrentVersion)
                .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.CreatedBy)
                .HasMaxLength(200);

            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(200);

            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false);

            entity.Property(e => e.DeletedBy)
                .HasMaxLength(200);

            entity.Property(e => e.DeletedReason)
                .HasMaxLength(500);

            entity.Property(e => e.IsArchived)
                .HasDefaultValue(false);

            // Indexes
            entity.HasIndex(e => e.TenantId)
                .HasDatabaseName("IX_Documents_TenantId");

            entity.HasIndex(e => e.IsDeleted)
                .HasDatabaseName("IX_Documents_IsDeleted");

            // Relationships
            entity.HasOne(e => e.Tenant)
                .WithMany(e => e.Documents)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.DocumentType)
                .WithMany(e => e.Documents)
                .HasForeignKey(e => e.DocumentTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Self-referencing relationship for versioning
            entity.HasOne(e => e.ParentDocument)
                .WithMany(e => e.ChildVersions)
                .HasForeignKey(e => e.ParentDocumentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
