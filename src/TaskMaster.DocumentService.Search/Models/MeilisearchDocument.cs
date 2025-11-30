using System.Text.Json.Serialization;

namespace TaskMaster.DocumentService.Search.Models;

/// <summary>
/// Document model for Meilisearch indexing.
/// </summary>
public class MeilisearchDocument
{
    /// <summary>
    /// Gets or sets the unique identifier (combination of tenant and document ID).
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the document identifier.
    /// </summary>
    [JsonPropertyName("documentId")]
    public long DocumentId { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    [JsonPropertyName("tenantId")]
    public int TenantId { get; set; }

    /// <summary>
    /// Gets or sets the document type identifier.
    /// </summary>
    [JsonPropertyName("documentTypeId")]
    public int DocumentTypeId { get; set; }

    /// <summary>
    /// Gets or sets the document type name.
    /// </summary>
    [JsonPropertyName("documentTypeName")]
    public string DocumentTypeName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the document title.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the document description.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the extracted text content.
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// Gets or sets the original file name.
    /// </summary>
    [JsonPropertyName("fileName")]
    public string? FileName { get; set; }

    /// <summary>
    /// Gets or sets the MIME type.
    /// </summary>
    [JsonPropertyName("mimeType")]
    public string? MimeType { get; set; }

    /// <summary>
    /// Gets or sets the file size in bytes.
    /// </summary>
    [JsonPropertyName("fileSizeBytes")]
    public long? FileSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the document tags.
    /// </summary>
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Gets or sets the document version.
    /// </summary>
    [JsonPropertyName("version")]
    public int Version { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is the current version.
    /// </summary>
    [JsonPropertyName("isCurrentVersion")]
    public bool IsCurrentVersion { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp (Unix epoch).
    /// </summary>
    [JsonPropertyName("createdAt")]
    public long CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user who created the document.
    /// </summary>
    [JsonPropertyName("createdBy")]
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the last update timestamp (Unix epoch).
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public long? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the document is archived.
    /// </summary>
    [JsonPropertyName("isArchived")]
    public bool IsArchived { get; set; }
}
