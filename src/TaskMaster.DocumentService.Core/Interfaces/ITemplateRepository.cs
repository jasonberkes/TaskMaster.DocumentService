using TaskMaster.DocumentService.Core.Entities;

namespace TaskMaster.DocumentService.Core.Interfaces;

/// <summary>
/// Repository interface for document template operations.
/// </summary>
public interface ITemplateRepository : IRepository<DocumentTemplate, int>
{
    /// <summary>
    /// Gets all templates for a specific tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="includeDeleted">Whether to include soft-deleted templates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of templates.</returns>
    Task<IEnumerable<DocumentTemplate>> GetByTenantIdAsync(int tenantId, bool includeDeleted = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a template by tenant ID and template name.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="templateName">The template name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The template if found, otherwise null.</returns>
    Task<DocumentTemplate?> GetByTenantAndNameAsync(int tenantId, string templateName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets templates by document type.
    /// </summary>
    /// <param name="documentTypeId">The document type identifier.</param>
    /// <param name="includeDeleted">Whether to include soft-deleted templates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of templates.</returns>
    Task<IEnumerable<DocumentTemplate>> GetByDocumentTypeIdAsync(int documentTypeId, bool includeDeleted = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active templates for a specific tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of active templates.</returns>
    Task<IEnumerable<DocumentTemplate>> GetActiveTemplatesAsync(int tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a template with its variables loaded.
    /// </summary>
    /// <param name="templateId">The template identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The template with variables, or null if not found.</returns>
    Task<DocumentTemplate?> GetTemplateWithVariablesAsync(int templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a template.
    /// </summary>
    /// <param name="templateId">The template identifier.</param>
    /// <param name="deletedBy">The user who deleted the template.</param>
    /// <param name="deletedReason">The reason for deletion.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SoftDeleteAsync(int templateId, string deletedBy, string? deletedReason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a soft-deleted template.
    /// </summary>
    /// <param name="templateId">The template identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RestoreAsync(int templateId, CancellationToken cancellationToken = default);
}
