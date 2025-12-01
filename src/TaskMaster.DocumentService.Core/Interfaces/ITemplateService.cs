using TaskMaster.DocumentService.Core.Entities;

namespace TaskMaster.DocumentService.Core.Interfaces;

/// <summary>
/// Service interface for document template management and document generation from templates.
/// </summary>
public interface ITemplateService
{
    /// <summary>
    /// Creates a new document template.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="documentTypeId">The document type identifier.</param>
    /// <param name="name">The template name.</param>
    /// <param name="description">The template description.</param>
    /// <param name="content">The template content with variable placeholders.</param>
    /// <param name="mimeType">The MIME type of the template content.</param>
    /// <param name="fileExtension">The file extension for documents created from this template.</param>
    /// <param name="variables">The variables defined in the template.</param>
    /// <param name="defaultTitlePattern">The default title pattern.</param>
    /// <param name="defaultDescriptionPattern">The default description pattern.</param>
    /// <param name="metadata">Optional metadata as JSON.</param>
    /// <param name="tags">Optional tags as JSON.</param>
    /// <param name="createdBy">The user creating the template.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created template.</returns>
    Task<DocumentTemplate> CreateTemplateAsync(
        int tenantId,
        int documentTypeId,
        string name,
        string description,
        string content,
        string mimeType,
        string? fileExtension,
        IEnumerable<TemplateVariable> variables,
        string? defaultTitlePattern,
        string? defaultDescriptionPattern,
        string? metadata,
        string? tags,
        string createdBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a template by its identifier.
    /// </summary>
    /// <param name="templateId">The template identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The template if found, otherwise null.</returns>
    Task<DocumentTemplate?> GetTemplateByIdAsync(int templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a template by tenant and name.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="templateName">The template name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The template if found, otherwise null.</returns>
    Task<DocumentTemplate?> GetTemplateByNameAsync(int tenantId, string templateName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all templates for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="includeDeleted">Whether to include soft-deleted templates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of templates.</returns>
    Task<IEnumerable<DocumentTemplate>> GetTemplatesByTenantAsync(int tenantId, bool includeDeleted = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active templates for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of active templates.</returns>
    Task<IEnumerable<DocumentTemplate>> GetActiveTemplatesAsync(int tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a template.
    /// </summary>
    /// <param name="templateId">The template identifier.</param>
    /// <param name="name">The updated template name.</param>
    /// <param name="description">The updated description.</param>
    /// <param name="content">The updated template content.</param>
    /// <param name="mimeType">The updated MIME type.</param>
    /// <param name="fileExtension">The updated file extension.</param>
    /// <param name="variables">The updated variables.</param>
    /// <param name="defaultTitlePattern">The updated default title pattern.</param>
    /// <param name="defaultDescriptionPattern">The updated default description pattern.</param>
    /// <param name="metadata">The updated metadata.</param>
    /// <param name="tags">The updated tags.</param>
    /// <param name="isActive">The updated active status.</param>
    /// <param name="updatedBy">The user updating the template.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated template.</returns>
    Task<DocumentTemplate> UpdateTemplateAsync(
        int templateId,
        string? name,
        string? description,
        string? content,
        string? mimeType,
        string? fileExtension,
        IEnumerable<TemplateVariable>? variables,
        string? defaultTitlePattern,
        string? defaultDescriptionPattern,
        string? metadata,
        string? tags,
        bool? isActive,
        string updatedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a template (soft delete).
    /// </summary>
    /// <param name="templateId">The template identifier.</param>
    /// <param name="deletedBy">The user deleting the template.</param>
    /// <param name="deletedReason">The reason for deletion.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteTemplateAsync(int templateId, string deletedBy, string? deletedReason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a soft-deleted template.
    /// </summary>
    /// <param name="templateId">The template identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RestoreTemplateAsync(int templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a document from a template with variable substitution.
    /// </summary>
    /// <param name="templateId">The template identifier.</param>
    /// <param name="variables">The variable values for substitution.</param>
    /// <param name="title">Optional custom title (overrides template default).</param>
    /// <param name="description">Optional custom description (overrides template default).</param>
    /// <param name="metadata">Optional custom metadata.</param>
    /// <param name="tags">Optional custom tags.</param>
    /// <param name="createdBy">The user creating the document.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created document.</returns>
    Task<Document> CreateDocumentFromTemplateAsync(
        int templateId,
        Dictionary<string, string> variables,
        string? title,
        string? description,
        string? metadata,
        string? tags,
        string createdBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates variable values against template variable definitions.
    /// </summary>
    /// <param name="template">The template with variable definitions.</param>
    /// <param name="variables">The variable values to validate.</param>
    /// <returns>A dictionary of validation errors (variable name -> error message).</returns>
    Dictionary<string, string> ValidateVariables(DocumentTemplate template, Dictionary<string, string> variables);

    /// <summary>
    /// Substitutes variables in content with provided values.
    /// </summary>
    /// <param name="content">The content with variable placeholders.</param>
    /// <param name="variables">The variable values for substitution.</param>
    /// <returns>The content with variables substituted.</returns>
    string SubstituteVariables(string content, Dictionary<string, string> variables);
}
