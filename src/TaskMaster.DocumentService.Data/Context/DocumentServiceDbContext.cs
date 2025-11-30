using Microsoft.EntityFrameworkCore;
using TaskMaster.DocumentService.Core.Entities;

namespace TaskMaster.DocumentService.Data.Context;

/// <summary>
/// Entity Framework Core database context for the Document Service.
/// </summary>
public class DocumentServiceDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentServiceDbContext"/> class.
    /// </summary>
    /// <param name="options">The options to be used by the DbContext.</param>
    public DocumentServiceDbContext(DbContextOptions<DocumentServiceDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the DocumentTemplates DbSet.
    /// </summary>
    public DbSet<DocumentTemplate> DocumentTemplates { get; set; } = null!;

    /// <summary>
    /// Gets or sets the TemplateUsageLog DbSet.
    /// </summary>
    public DbSet<TemplateUsageLog> TemplateUsageLog { get; set; } = null!;

    /// <summary>
    /// Configures the entity mappings and relationships.
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure DocumentTemplate entity
        modelBuilder.Entity<DocumentTemplate>(entity =>
        {
            entity.ToTable("DocumentTemplates", "docs");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Description)
                .HasMaxLength(2000);

            entity.Property(e => e.Content)
                .IsRequired();

            entity.Property(e => e.TemplateType)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Category)
                .HasMaxLength(100);

            entity.Property(e => e.CreatedBy)
                .HasMaxLength(200);

            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(200);

            entity.Property(e => e.DeletedBy)
                .HasMaxLength(200);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // Self-referencing relationship for versioning
            entity.HasOne(e => e.ParentTemplate)
                .WithMany()
                .HasForeignKey(e => e.ParentTemplateId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.IsDeleted);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.TemplateType);
            entity.HasIndex(e => e.Name);
        });

        // Configure TemplateUsageLog entity
        modelBuilder.Entity<TemplateUsageLog>(entity =>
        {
            entity.ToTable("TemplateUsageLog", "docs");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("Success");

            entity.Property(e => e.UsedBy)
                .HasMaxLength(200);

            entity.Property(e => e.ErrorMessage)
                .HasMaxLength(2000);

            entity.Property(e => e.UsedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // Relationship with DocumentTemplate
            entity.HasOne(e => e.Template)
                .WithMany()
                .HasForeignKey(e => e.TemplateId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            entity.HasIndex(e => e.TemplateId);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.UsedAt);
        });
    }
}
