using TaskMaster.DocumentService.Core.DTOs;

namespace TaskMaster.DocumentService.Core.Interfaces;

/// <summary>
/// Service interface for document business logic operations
/// </summary>
public interface IDocumentService
{
    /// <summary>
    /// Gets a document by its identifier
    /// </summary>
    /// <param name="id">The document identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The document DTO if found, null otherwise</returns>
    Task<DocumentDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets documents by tenant identifier with pagination
    /// </summary>
    /// <param name="tenantId">The tenant identifier</param>
    /// <param name="skip">Number of records to skip</param>
    /// <param name="take">Number of records to take</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of document DTOs</returns>
    Task<IEnumerable<DocumentDto>> GetByTenantIdAsync(int tenantId, int skip = 0, int take = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all versions of a document
    /// </summary>
    /// <param name="documentId">The document identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of document versions ordered by version number</returns>
    Task<IEnumerable<DocumentDto>> GetVersionsAsync(long documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new document
    /// </summary>
    /// <param name="createDto">The document creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created document DTO</returns>
    Task<DocumentDto> CreateAsync(CreateDocumentDto createDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new version of an existing document
    /// </summary>
    /// <param name="documentId">The original document identifier</param>
    /// <param name="versionDto">The version creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created version DTO</returns>
    Task<DocumentDto> CreateVersionAsync(long documentId, CreateDocumentVersionDto versionDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates document metadata
    /// </summary>
    /// <param name="id">The document identifier</param>
    /// <param name="updateDto">The update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated document DTO if found, null otherwise</returns>
    Task<DocumentDto?> UpdateAsync(long id, UpdateDocumentDto updateDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes a document
    /// </summary>
    /// <param name="id">The document identifier</param>
    /// <param name="deletedBy">The user performing the deletion</param>
    /// <param name="deletedReason">The reason for deletion</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully, false if not found</returns>
    Task<bool> DeleteAsync(long id, string? deletedBy, string? deletedReason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Archives a document
    /// </summary>
    /// <param name="id">The document identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if archived successfully, false if not found</returns>
    Task<bool> ArchiveAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unarchives a document
    /// </summary>
    /// <param name="id">The document identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if unarchived successfully, false if not found</returns>
    Task<bool> UnarchiveAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of documents for a tenant
    /// </summary>
    /// <param name="tenantId">The tenant identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The count of documents</returns>
    Task<int> GetCountByTenantIdAsync(int tenantId, CancellationToken cancellationToken = default);
}
