using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskMaster.DocumentService.Core.Entities;

/// <summary>
/// Unified metadata table tracking ALL content stored in Azure Blob Storage.
/// WI #1145: Create Unified BlobMetadata System
/// WI #3660: DocumentService owns this in 'documents' schema
/// </summary>
[Table("BlobMetadata", Schema = "documents")]
public class BlobMetadata
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    // ===== Blob Location =====
    [Required]
    [MaxLength(100)]
    public string ContainerName { get; set; } = "taskmaster-documents";

    [Required]
    [MaxLength(500)]
    public string BlobName { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string BlobPath { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string BlobUrl { get; set; } = string.Empty;

    // ===== Content Classification =====
    [Required]
    [MaxLength(50)]
    public string ContentType { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Category { get; set; }

    [MaxLength(100)]
    public string? Subcategory { get; set; }

    // ===== Metadata =====
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Summary { get; set; }

    [MaxLength(500)]
    public string? Tags { get; set; }

    // ===== Relationships =====
    public int? WorkItemId { get; set; }
    public int? PrNumber { get; set; }

    [Required]
    public int OrganizationId { get; set; } = 1;

    public int? ProjectId { get; set; }
    public int? ParentId { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? ExtendedMetadata { get; set; }

    // ===== Indexing Status =====
    [Required]
    public bool IsIndexed { get; set; } = false;

    public DateTime? LastIndexedAt { get; set; }

    [MaxLength(64)]
    public string? ContentHash { get; set; }

    [MaxLength(100)]
    public string? MeilisearchDocumentId { get; set; }

    // ===== File Info =====
    public long? ContentLength { get; set; }

    [MaxLength(100)]
    public string? MimeType { get; set; }

    // ===== Audit =====
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [MaxLength(100)]
    public string? UpdatedBy { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    // ===== Non-persisted =====
    [NotMapped]
    public string? TextContent { get; set; }
}
