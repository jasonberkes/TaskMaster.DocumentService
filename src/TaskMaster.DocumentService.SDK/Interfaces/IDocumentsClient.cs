using TaskMaster.DocumentService.SDK.DTOs;

namespace TaskMaster.DocumentService.SDK.Interfaces;

/// <summary>
/// Client interface for document operations.
/// </summary>
public interface IDocumentsClient
{
    /// <summary>
    /// Gets a document by its identifier.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The document if found.</returns>
    Task<DocumentDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all documents for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="includeDeleted">Whether to include soft-deleted documents.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of documents.</returns>
    Task<IEnumerable<DocumentDto>> GetByTenantIdAsync(int tenantId, bool includeDeleted = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets documents by document type.
    /// </summary>
    /// <param name="documentTypeId">The document type identifier.</param>
    /// <param name="includeDeleted">Whether to include soft-deleted documents.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of documents.</returns>
    Task<IEnumerable<DocumentDto>> GetByDocumentTypeIdAsync(int documentTypeId, bool includeDeleted = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current version of a document.
    /// </summary>
    /// <param name="parentDocumentId">The parent document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current version of the document.</returns>
    Task<DocumentDto?> GetCurrentVersionAsync(long parentDocumentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all versions of a document.
    /// </summary>
    /// <param name="parentDocumentId">The parent document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of document versions.</returns>
    Task<IEnumerable<DocumentDto>> GetVersionsAsync(long parentDocumentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets archived documents for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of archived documents.</returns>
    Task<IEnumerable<DocumentDto>> GetArchivedAsync(int tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new document.
    /// </summary>
    /// <param name="request">The create document request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created document.</returns>
    Task<DocumentDto> CreateAsync(CreateDocumentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing document.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="request">The update document request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated document.</returns>
    Task<DocumentDto> UpdateAsync(long id, UpdateDocumentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a document.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="deletedBy">The user who is deleting the document.</param>
    /// <param name="deletedReason">The reason for deletion.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(long id, string deletedBy, string? deletedReason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a soft-deleted document.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RestoreAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Archives a document.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ArchiveAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a document with content.
    /// </summary>
    /// <param name="request">The create document request.</param>
    /// <param name="content">The document content stream.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created document.</returns>
    Task<DocumentDto> UploadAsync(CreateDocumentRequest request, Stream content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a document's content.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The document content stream.</returns>
    Task<Stream> DownloadAsync(long id, CancellationToken cancellationToken = default);
}
