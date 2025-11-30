using TaskMaster.DocumentService.Core.Entities;

namespace TaskMaster.DocumentService.Core.Interfaces;

/// <summary>
/// Repository interface for DocumentTemplate entity operations.
/// </summary>
public interface IDocumentTemplateRepository : IRepository<DocumentTemplate, long>
{
    /// <summary>
    /// Gets templates by tenant identifier.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="includeDeleted">Whether to include soft-deleted templates.</param>
    /// <param name="includeInactive">Whether to include inactive templates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of templates belonging to the tenant.</returns>
    Task<IEnumerable<DocumentTemplate>> GetByTenantIdAsync(
        int tenantId,
        bool includeDeleted = false,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets templates by document type identifier.
    /// </summary>
    /// <param name="documentTypeId">The document type identifier.</param>
    /// <param name="includeDeleted">Whether to include soft-deleted templates.</param>
    /// <param name="includeInactive">Whether to include inactive templates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of templates for the specified document type.</returns>
    Task<IEnumerable<DocumentTemplate>> GetByDocumentTypeIdAsync(
        int documentTypeId,
        bool includeDeleted = false,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets templates by category.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="category">The category name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of templates in the specified category.</returns>
    Task<IEnumerable<DocumentTemplate>> GetByCategoryAsync(
        int tenantId,
        string category,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current version of a template by its parent template identifier.
    /// </summary>
    /// <param name="parentTemplateId">The parent template identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current version of the template if found, otherwise null.</returns>
    Task<DocumentTemplate?> GetCurrentVersionAsync(long parentTemplateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all versions of a template.
    /// </summary>
    /// <param name="parentTemplateId">The parent template identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of all template versions ordered by version number descending.</returns>
    Task<IEnumerable<DocumentTemplate>> GetVersionsAsync(long parentTemplateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active templates for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of active templates.</returns>
    Task<IEnumerable<DocumentTemplate>> GetActiveTemplatesAsync(int tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a template.
    /// </summary>
    /// <param name="templateId">The template identifier.</param>
    /// <param name="deletedBy">The user who is deleting the template.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SoftDeleteAsync(long templateId, string deletedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a soft-deleted template.
    /// </summary>
    /// <param name="templateId">The template identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RestoreAsync(long templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches templates by name.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="searchTerm">The search term.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of matching templates.</returns>
    Task<IEnumerable<DocumentTemplate>> SearchByNameAsync(
        int tenantId,
        string searchTerm,
        CancellationToken cancellationToken = default);
}
