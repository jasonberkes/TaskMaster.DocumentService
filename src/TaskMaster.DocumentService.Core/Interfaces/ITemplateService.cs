using TaskMaster.DocumentService.Core.Entities;

namespace TaskMaster.DocumentService.Core.Interfaces;

/// <summary>
/// Service interface for document template management and document generation with variable substitution.
/// </summary>
public interface ITemplateService
{
    /// <summary>
    /// Creates a new document template with content upload to blob storage.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="documentTypeId">The document type identifier.</param>
    /// <param name="name">The template name.</param>
    /// <param name="description">The template description.</param>
    /// <param name="content">The template content stream.</param>
    /// <param name="fileName">The original file name.</param>
    /// <param name="contentType">The MIME type of the template.</param>
    /// <param name="availableVariables">Optional list of available variables as JSON array.</param>
    /// <param name="metadata">Optional metadata as JSON string.</param>
    /// <param name="category">Optional category for organizing templates.</param>
    /// <param name="createdBy">The user creating the template.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created template.</returns>
    Task<DocumentTemplate> CreateTemplateAsync(
        int tenantId,
        int documentTypeId,
        string name,
        string? description,
        Stream content,
        string fileName,
        string contentType,
        string? availableVariables,
        string? metadata,
        string? category,
        string createdBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a template by its identifier.
    /// </summary>
    /// <param name="templateId">The template identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The template if found, otherwise null.</returns>
    Task<DocumentTemplate?> GetTemplateByIdAsync(long templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all templates for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="includeDeleted">Whether to include soft-deleted templates.</param>
    /// <param name="includeInactive">Whether to include inactive templates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of templates.</returns>
    Task<IEnumerable<DocumentTemplate>> GetTemplatesByTenantAsync(
        int tenantId,
        bool includeDeleted = false,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets templates by document type.
    /// </summary>
    /// <param name="documentTypeId">The document type identifier.</param>
    /// <param name="includeDeleted">Whether to include soft-deleted templates.</param>
    /// <param name="includeInactive">Whether to include inactive templates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of templates.</returns>
    Task<IEnumerable<DocumentTemplate>> GetTemplatesByTypeAsync(
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
    Task<IEnumerable<DocumentTemplate>> GetTemplatesByCategoryAsync(
        int tenantId,
        string category,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches templates by name.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="searchTerm">The search term.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of matching templates.</returns>
    Task<IEnumerable<DocumentTemplate>> SearchTemplatesAsync(
        int tenantId,
        string searchTerm,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates template metadata and properties.
    /// </summary>
    /// <param name="templateId">The template identifier.</param>
    /// <param name="name">The updated name.</param>
    /// <param name="description">The updated description.</param>
    /// <param name="availableVariables">The updated available variables as JSON array.</param>
    /// <param name="metadata">The updated metadata as JSON string.</param>
    /// <param name="category">The updated category.</param>
    /// <param name="isActive">Whether the template is active.</param>
    /// <param name="updatedBy">The user updating the template.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated template.</returns>
    Task<DocumentTemplate> UpdateTemplateMetadataAsync(
        long templateId,
        string? name,
        string? description,
        string? availableVariables,
        string? metadata,
        string? category,
        bool? isActive,
        string updatedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads template content from blob storage.
    /// </summary>
    /// <param name="templateId">The template identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The template content stream.</returns>
    Task<Stream> DownloadTemplateAsync(long templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a document from a template with variable substitution.
    /// Variables in the template should use the format {{variableName}}.
    /// </summary>
    /// <param name="templateId">The template identifier to use.</param>
    /// <param name="title">The title for the generated document.</param>
    /// <param name="description">The description for the generated document.</param>
    /// <param name="variables">Dictionary of variable names and their substitution values.</param>
    /// <param name="metadata">Optional metadata for the generated document as JSON string.</param>
    /// <param name="tags">Optional tags for the generated document as JSON string.</param>
    /// <param name="createdBy">The user generating the document.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated document.</returns>
    Task<Document> GenerateDocumentFromTemplateAsync(
        long templateId,
        string title,
        string? description,
        Dictionary<string, string> variables,
        string? metadata,
        string? tags,
        string createdBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a template.
    /// </summary>
    /// <param name="templateId">The template identifier.</param>
    /// <param name="deletedBy">The user deleting the template.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteTemplateAsync(
        long templateId,
        string deletedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a soft-deleted template.
    /// </summary>
    /// <param name="templateId">The template identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RestoreTemplateAsync(long templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently deletes a template and its blob storage content.
    /// </summary>
    /// <param name="templateId">The template identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PermanentlyDeleteTemplateAsync(long templateId, CancellationToken cancellationToken = default);
}
