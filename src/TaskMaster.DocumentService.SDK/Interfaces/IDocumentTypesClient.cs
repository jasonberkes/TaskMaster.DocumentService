using TaskMaster.DocumentService.SDK.DTOs;

namespace TaskMaster.DocumentService.SDK.Interfaces;

/// <summary>
/// Client interface for document type operations.
/// </summary>
public interface IDocumentTypesClient
{
    /// <summary>
    /// Gets a document type by its identifier.
    /// </summary>
    /// <param name="id">The document type identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The document type if found.</returns>
    Task<DocumentTypeDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a document type by its name.
    /// </summary>
    /// <param name="name">The document type name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The document type if found.</returns>
    Task<DocumentTypeDto?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all document types.
    /// </summary>
    /// <param name="activeOnly">Whether to return only active document types.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of document types.</returns>
    Task<IEnumerable<DocumentTypeDto>> GetAllAsync(bool activeOnly = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new document type.
    /// </summary>
    /// <param name="documentType">The document type to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created document type.</returns>
    Task<DocumentTypeDto> CreateAsync(DocumentTypeDto documentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing document type.
    /// </summary>
    /// <param name="id">The document type identifier.</param>
    /// <param name="documentType">The updated document type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated document type.</returns>
    Task<DocumentTypeDto> UpdateAsync(int id, DocumentTypeDto documentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document type.
    /// </summary>
    /// <param name="id">The document type identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
