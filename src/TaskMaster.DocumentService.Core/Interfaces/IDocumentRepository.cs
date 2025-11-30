using TaskMaster.DocumentService.Core.Entities;

namespace TaskMaster.DocumentService.Core.Interfaces;

/// <summary>
/// Repository interface for document operations.
/// </summary>
public interface IDocumentRepository
{
    /// <summary>
    /// Gets a document by ID and tenant ID.
    /// </summary>
    /// <param name="id">The document ID.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <returns>The document, or null if not found.</returns>
    Task<Document?> GetByIdAsync(Guid id, Guid tenantId);

    /// <summary>
    /// Gets all documents for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="includeDeleted">Whether to include soft-deleted documents.</param>
    /// <returns>A collection of documents.</returns>
    Task<IEnumerable<Document>> GetByTenantAsync(Guid tenantId, bool includeDeleted = false);

    /// <summary>
    /// Creates a new document.
    /// </summary>
    /// <param name="document">The document to create.</param>
    /// <returns>The created document.</returns>
    Task<Document> CreateAsync(Document document);

    /// <summary>
    /// Updates an existing document.
    /// </summary>
    /// <param name="document">The document to update.</param>
    /// <returns>The updated document.</returns>
    Task<Document> UpdateAsync(Document document);

    /// <summary>
    /// Soft deletes a document.
    /// </summary>
    /// <param name="id">The document ID.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <returns>True if the document was deleted, false otherwise.</returns>
    Task<bool> DeleteAsync(Guid id, Guid tenantId);

    /// <summary>
    /// Checks if a document exists for a tenant.
    /// </summary>
    /// <param name="id">The document ID.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <returns>True if the document exists, false otherwise.</returns>
    Task<bool> ExistsAsync(Guid id, Guid tenantId);
}
