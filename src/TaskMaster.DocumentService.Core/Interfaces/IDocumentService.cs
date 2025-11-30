using TaskMaster.DocumentService.Core.Entities;

namespace TaskMaster.DocumentService.Core.Interfaces;

/// <summary>
/// Service interface for document management operations with business logic.
/// </summary>
public interface IDocumentService
{
    /// <summary>
    /// Creates a new document with content upload to blob storage.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="documentTypeId">The document type identifier.</param>
    /// <param name="title">The document title.</param>
    /// <param name="description">The document description.</param>
    /// <param name="content">The document content stream.</param>
    /// <param name="fileName">The original file name.</param>
    /// <param name="contentType">The MIME type of the document.</param>
    /// <param name="metadata">Optional metadata as JSON string.</param>
    /// <param name="tags">Optional tags as JSON string.</param>
    /// <param name="createdBy">The user creating the document.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created document.</returns>
    Task<Document> CreateDocumentAsync(
        int tenantId,
        int documentTypeId,
        string title,
        string? description,
        Stream content,
        string fileName,
        string contentType,
        string? metadata,
        string? tags,
        string createdBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a document by its identifier.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The document if found, otherwise null.</returns>
    Task<Document?> GetDocumentByIdAsync(long documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all documents for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="includeDeleted">Whether to include soft-deleted documents.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of documents.</returns>
    Task<IEnumerable<Document>> GetDocumentsByTenantAsync(
        int tenantId,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets documents by document type.
    /// </summary>
    /// <param name="documentTypeId">The document type identifier.</param>
    /// <param name="includeDeleted">Whether to include soft-deleted documents.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of documents.</returns>
    Task<IEnumerable<Document>> GetDocumentsByTypeAsync(
        int documentTypeId,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates document metadata and properties.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="title">The updated title.</param>
    /// <param name="description">The updated description.</param>
    /// <param name="metadata">The updated metadata as JSON string.</param>
    /// <param name="tags">The updated tags as JSON string.</param>
    /// <param name="updatedBy">The user updating the document.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated document.</returns>
    Task<Document> UpdateDocumentMetadataAsync(
        long documentId,
        string? title,
        string? description,
        string? metadata,
        string? tags,
        string updatedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new version of an existing document.
    /// </summary>
    /// <param name="parentDocumentId">The parent document identifier.</param>
    /// <param name="content">The new document content stream.</param>
    /// <param name="fileName">The file name for the new version.</param>
    /// <param name="contentType">The MIME type of the new version.</param>
    /// <param name="updatedBy">The user creating the new version.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new document version.</returns>
    Task<Document> CreateDocumentVersionAsync(
        long parentDocumentId,
        Stream content,
        string fileName,
        string contentType,
        string updatedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all versions of a document.
    /// </summary>
    /// <param name="parentDocumentId">The parent document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of all document versions.</returns>
    Task<IEnumerable<Document>> GetDocumentVersionsAsync(
        long parentDocumentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current version of a document.
    /// </summary>
    /// <param name="parentDocumentId">The parent document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current version if found, otherwise null.</returns>
    Task<Document?> GetCurrentVersionAsync(
        long parentDocumentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads document content from blob storage.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The document content stream.</returns>
    Task<Stream> DownloadDocumentAsync(long documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a temporary SAS URI for document access.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="expiresIn">The duration for which the SAS token is valid.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The SAS URI for temporary access.</returns>
    Task<string> GetDocumentSasUriAsync(
        long documentId,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a document.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="deletedBy">The user deleting the document.</param>
    /// <param name="deletedReason">The reason for deletion.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteDocumentAsync(
        long documentId,
        string deletedBy,
        string? deletedReason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a soft-deleted document.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RestoreDocumentAsync(long documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Archives a document.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ArchiveDocumentAsync(long documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all archived documents for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of archived documents.</returns>
    Task<IEnumerable<Document>> GetArchivedDocumentsAsync(
        int tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently deletes a document and its blob storage content.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PermanentlyDeleteDocumentAsync(long documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a document with the same content already exists (deduplication).
    /// </summary>
    /// <param name="contentHash">The content hash.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of documents with the same content hash.</returns>
    Task<IEnumerable<Document>> FindDuplicateDocumentsAsync(
        string contentHash,
        CancellationToken cancellationToken = default);
}
