using TaskMaster.DocumentService.Core.DTOs;

namespace TaskMaster.DocumentService.Core.Interfaces;

/// <summary>
/// Service interface for document operations.
/// </summary>
public interface IDocumentService
{
    /// <summary>
    /// Gets a document by ID for a specific tenant.
    /// </summary>
    /// <param name="id">The document ID.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <returns>The document DTO, or null if not found.</returns>
    Task<DocumentDto?> GetDocumentAsync(Guid id, Guid tenantId);

    /// <summary>
    /// Gets all documents for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <returns>A collection of document DTOs.</returns>
    Task<IEnumerable<DocumentDto>> GetTenantDocumentsAsync(Guid tenantId);

    /// <summary>
    /// Creates a new document.
    /// </summary>
    /// <param name="createDto">The document creation data.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="userId">The user ID who is creating the document.</param>
    /// <returns>The created document DTO.</returns>
    Task<DocumentDto> CreateDocumentAsync(CreateDocumentDto createDto, Guid tenantId, string userId);

    /// <summary>
    /// Updates an existing document.
    /// </summary>
    /// <param name="id">The document ID.</param>
    /// <param name="updateDto">The document update data.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <returns>The updated document DTO, or null if not found.</returns>
    Task<DocumentDto?> UpdateDocumentAsync(Guid id, UpdateDocumentDto updateDto, Guid tenantId);

    /// <summary>
    /// Deletes a document (soft delete).
    /// </summary>
    /// <param name="id">The document ID.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <returns>True if the document was deleted, false otherwise.</returns>
    Task<bool> DeleteDocumentAsync(Guid id, Guid tenantId);
}
