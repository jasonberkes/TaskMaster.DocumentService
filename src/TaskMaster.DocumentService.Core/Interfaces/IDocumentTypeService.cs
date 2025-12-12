using TaskMaster.DocumentService.Core.Entities;

namespace TaskMaster.DocumentService.Core.Interfaces;

/// <summary>
/// Service interface for document type management operations.
/// </summary>
public interface IDocumentTypeService
{
    /// <summary>
    /// Gets a document type by its unique identifier.
    /// </summary>
    /// <param name="id">The document type identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The document type if found, otherwise null.</returns>
    Task<DocumentType?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a document type by its name.
    /// </summary>
    /// <param name="name">The document type name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The document type if found, otherwise null.</returns>
    Task<DocumentType?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all document types.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of all document types.</returns>
    Task<IEnumerable<DocumentType>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active document types.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of active document types.</returns>
    Task<IEnumerable<DocumentType>> GetActiveDocumentTypesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets document types that have content indexing enabled.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of indexable document types.</returns>
    Task<IEnumerable<DocumentType>> GetIndexableTypesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets document types that have extension tables.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of document types with extension tables.</returns>
    Task<IEnumerable<DocumentType>> GetTypesWithExtensionTablesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new document type.
    /// </summary>
    /// <param name="documentType">The document type to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created document type with assigned identifier.</returns>
    /// <exception cref="ArgumentNullException">Thrown when documentType is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a document type with the same name already exists.</exception>
    Task<DocumentType> CreateAsync(DocumentType documentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing document type.
    /// </summary>
    /// <param name="documentType">The document type to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated document type.</returns>
    /// <exception cref="ArgumentNullException">Thrown when documentType is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when document type does not exist or name conflicts with another document type.</exception>
    Task<DocumentType> UpdateAsync(DocumentType documentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document type by its identifier.
    /// </summary>
    /// <param name="id">The document type identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the document type was deleted, false if it didn't exist.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the document type is in use by documents.</exception>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a document type with the specified name exists.
    /// </summary>
    /// <param name="name">The document type name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if a document type with the name exists, false otherwise.</returns>
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);
}
