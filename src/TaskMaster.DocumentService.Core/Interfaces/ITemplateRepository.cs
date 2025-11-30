using TaskMaster.DocumentService.Core.Entities;

namespace TaskMaster.DocumentService.Core.Interfaces;

/// <summary>
/// Repository interface for managing document templates.
/// </summary>
public interface ITemplateRepository
{
    /// <summary>
    /// Gets a template by its identifier.
    /// </summary>
    /// <param name="id">The template identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The template if found, otherwise null.</returns>
    Task<DocumentTemplate?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active templates for a specific tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="includePublic">Whether to include public templates from parent tenants.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of templates.</returns>
    Task<List<DocumentTemplate>> GetByTenantIdAsync(int tenantId, bool includePublic = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets templates by type for a specific tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="templateType">The template type (e.g., Document, Email, Report).</param>
    /// <param name="includePublic">Whether to include public templates.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of templates.</returns>
    Task<List<DocumentTemplate>> GetByTypeAsync(int tenantId, string templateType, bool includePublic = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches templates by name for a specific tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="searchTerm">The search term to match against template names.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of matching templates.</returns>
    Task<List<DocumentTemplate>> SearchByNameAsync(int tenantId, string searchTerm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new template.
    /// </summary>
    /// <param name="template">The template to create.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created template with generated identifier.</returns>
    Task<DocumentTemplate> CreateAsync(DocumentTemplate template, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing template.
    /// </summary>
    /// <param name="template">The template to update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated template.</returns>
    Task<DocumentTemplate> UpdateAsync(DocumentTemplate template, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes a template by setting IsDeleted flag.
    /// </summary>
    /// <param name="id">The template identifier.</param>
    /// <param name="deletedBy">The user performing the deletion.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the template was deleted, otherwise false.</returns>
    Task<bool> DeleteAsync(int id, string deletedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs template usage for analytics and auditing.
    /// </summary>
    /// <param name="usageLog">The usage log entry.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created usage log entry.</returns>
    Task<TemplateUsageLog> LogUsageAsync(TemplateUsageLog usageLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets usage statistics for a template.
    /// </summary>
    /// <param name="templateId">The template identifier.</param>
    /// <param name="startDate">The start date for the statistics.</param>
    /// <param name="endDate">The end date for the statistics.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of usage log entries.</returns>
    Task<List<TemplateUsageLog>> GetUsageStatisticsAsync(int templateId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
}
