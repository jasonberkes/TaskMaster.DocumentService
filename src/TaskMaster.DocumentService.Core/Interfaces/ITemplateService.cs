using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Models;

namespace TaskMaster.DocumentService.Core.Interfaces;

/// <summary>
/// Service interface for template management and variable substitution.
/// </summary>
public interface ITemplateService
{
    /// <summary>
    /// Gets a template by its identifier.
    /// </summary>
    /// <param name="id">The template identifier.</param>
    /// <param name="tenantId">The tenant identifier for authorization.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The template if found and authorized, otherwise null.</returns>
    Task<DocumentTemplate?> GetTemplateAsync(int id, int tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all templates for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="includePublic">Whether to include public templates.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of templates.</returns>
    Task<List<DocumentTemplate>> GetTemplatesAsync(int tenantId, bool includePublic = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets templates by type.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="templateType">The template type.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of templates.</returns>
    Task<List<DocumentTemplate>> GetTemplatesByTypeAsync(int tenantId, string templateType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches templates by name.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="searchTerm">The search term.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of matching templates.</returns>
    Task<List<DocumentTemplate>> SearchTemplatesAsync(int tenantId, string searchTerm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new template.
    /// </summary>
    /// <param name="template">The template to create.</param>
    /// <param name="createdBy">The user creating the template.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created template.</returns>
    Task<DocumentTemplate> CreateTemplateAsync(DocumentTemplate template, string createdBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing template.
    /// </summary>
    /// <param name="template">The template to update.</param>
    /// <param name="updatedBy">The user updating the template.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated template.</returns>
    Task<DocumentTemplate> UpdateTemplateAsync(DocumentTemplate template, string updatedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a template.
    /// </summary>
    /// <param name="id">The template identifier.</param>
    /// <param name="tenantId">The tenant identifier for authorization.</param>
    /// <param name="deletedBy">The user deleting the template.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if deleted, otherwise false.</returns>
    Task<bool> DeleteTemplateAsync(int id, int tenantId, string deletedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Renders a template with variable substitution.
    /// </summary>
    /// <param name="templateId">The template identifier.</param>
    /// <param name="tenantId">The tenant identifier for authorization.</param>
    /// <param name="variables">The dictionary of variable values for substitution.</param>
    /// <param name="usedBy">The user rendering the template.</param>
    /// <param name="documentId">Optional document identifier if creating a document.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The render result with substituted content.</returns>
    Task<TemplateRenderResult> RenderTemplateAsync(int templateId, int tenantId, Dictionary<string, string> variables, string usedBy, long? documentId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Parses template variables from the template definition.
    /// </summary>
    /// <param name="variablesJson">The JSON string containing variable definitions.</param>
    /// <returns>A list of template variables.</returns>
    List<TemplateVariable> ParseTemplateVariables(string? variablesJson);

    /// <summary>
    /// Validates template content and variable definitions.
    /// </summary>
    /// <param name="content">The template content.</param>
    /// <param name="variables">The variable definitions.</param>
    /// <returns>A list of validation errors, empty if valid.</returns>
    List<string> ValidateTemplate(string content, List<TemplateVariable> variables);
}
