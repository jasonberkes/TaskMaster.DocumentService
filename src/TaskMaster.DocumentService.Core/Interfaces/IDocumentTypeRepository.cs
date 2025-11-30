using TaskMaster.DocumentService.Core.Entities;

namespace TaskMaster.DocumentService.Core.Interfaces;

/// <summary>
/// Repository interface for DocumentType entity operations.
/// </summary>
public interface IDocumentTypeRepository : IRepository<DocumentType, int>
{
    /// <summary>
    /// Gets a document type by its name.
    /// </summary>
    /// <param name="name">The document type name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The document type if found, otherwise null.</returns>
    Task<DocumentType?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

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
}
