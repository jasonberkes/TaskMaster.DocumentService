using Microsoft.EntityFrameworkCore;
using TaskMaster.DocumentService.Core.Entities;

namespace TaskMaster.DocumentService.Data.Data;

/// <summary>
/// Entity Framework Core database context for the Document Service.
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
    /// Gets or sets the Collections DbSet.
    /// </summary>
    public DbSet<Collection> Collections { get; set; } = null!;

    /// <summary>
    /// Gets or sets the CollectionDocuments DbSet.
    /// </summary>
    public DbSet<CollectionDocument> CollectionDocuments { get; set; } = null!;

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Collection entity
        modelBuilder.Entity<Collection>(entity =>
        {
            entity.ToTable("Collections", "docs");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Description)
                .HasMaxLength(2000);

            entity.Property(e => e.Slug)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("Draft");

            entity.Property(e => e.CoverImageUrl)
                .HasMaxLength(500);

            entity.Property(e => e.PublishedBy)
                .HasMaxLength(200);

            entity.Property(e => e.CreatedBy)
                .HasMaxLength(200);

            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(200);

            entity.Property(e => e.DeletedBy)
                .HasMaxLength(200);

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(e => e.IsPublished)
                .IsRequired()
                .HasDefaultValue(false);

            entity.HasIndex(e => e.TenantId)
                .HasDatabaseName("IX_Collections_TenantId");

            entity.HasIndex(e => e.Status)
                .HasDatabaseName("IX_Collections_Status");

            entity.HasIndex(e => e.IsPublished)
                .HasDatabaseName("IX_Collections_IsPublished");

            entity.HasIndex(e => e.IsDeleted)
                .HasDatabaseName("IX_Collections_IsDeleted");

            entity.HasIndex(e => new { e.TenantId, e.Slug })
                .HasDatabaseName("IX_Collections_TenantId_Slug")
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            // Configure navigation property
            entity.HasMany(e => e.CollectionDocuments)
                .WithOne(e => e.Collection)
                .HasForeignKey(e => e.CollectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure CollectionDocument entity
        modelBuilder.Entity<CollectionDocument>(entity =>
        {
            entity.ToTable("CollectionDocuments", "docs");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.AddedBy)
                .HasMaxLength(200);

            entity.Property(e => e.Notes)
                .HasMaxLength(1000);

            entity.Property(e => e.AddedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(e => e.CollectionId)
                .HasDatabaseName("IX_CollectionDocuments_CollectionId");

            entity.HasIndex(e => e.DocumentId)
                .HasDatabaseName("IX_CollectionDocuments_DocumentId");

            entity.HasIndex(e => new { e.CollectionId, e.DocumentId })
                .HasDatabaseName("IX_CollectionDocuments_CollectionId_DocumentId")
                .IsUnique();
        });
    }
}
