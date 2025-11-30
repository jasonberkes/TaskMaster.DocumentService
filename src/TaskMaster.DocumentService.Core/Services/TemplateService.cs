using System.Text.Json;
using System.Text.RegularExpressions;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Interfaces;
using TaskMaster.DocumentService.Core.Models;

namespace TaskMaster.DocumentService.Core.Services;

/// <summary>
/// Service implementation for template management and variable substitution.
/// </summary>
public partial class TemplateService : ITemplateService
{
    private readonly ITemplateRepository _repository;
    private static readonly Regex VariablePattern = VariablePlaceholderRegex();

    /// <summary>
    /// Initializes a new instance of the <see cref="TemplateService"/> class.
    /// </summary>
    /// <param name="repository">The template repository.</param>
    public TemplateService(ITemplateRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <inheritdoc />
    public async Task<DocumentTemplate?> GetTemplateAsync(int id, int tenantId, CancellationToken cancellationToken = default)
    {
        var template = await _repository.GetByIdAsync(id, cancellationToken);

        if (template == null)
        {
            return null;
        }

        // Authorization check: template must belong to tenant or be public
        if (template.TenantId != tenantId && !template.IsPublic)
        {
            return null;
        }

        return template;
    }

    /// <inheritdoc />
    public async Task<List<DocumentTemplate>> GetTemplatesAsync(int tenantId, bool includePublic = true, CancellationToken cancellationToken = default)
    {
        return await _repository.GetByTenantIdAsync(tenantId, includePublic, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<DocumentTemplate>> GetTemplatesByTypeAsync(int tenantId, string templateType, CancellationToken cancellationToken = default)
    {
        return await _repository.GetByTypeAsync(tenantId, templateType, true, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<DocumentTemplate>> SearchTemplatesAsync(int tenantId, string searchTerm, CancellationToken cancellationToken = default)
    {
        return await _repository.SearchByNameAsync(tenantId, searchTerm, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DocumentTemplate> CreateTemplateAsync(DocumentTemplate template, string createdBy, CancellationToken cancellationToken = default)
    {
        template.CreatedBy = createdBy;
        template.CreatedAt = DateTime.UtcNow;
        template.Version = 1;
        template.IsCurrentVersion = true;
        template.IsActive = true;
        template.IsDeleted = false;

        return await _repository.CreateAsync(template, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DocumentTemplate> UpdateTemplateAsync(DocumentTemplate template, string updatedBy, CancellationToken cancellationToken = default)
    {
        template.UpdatedBy = updatedBy;
        template.UpdatedAt = DateTime.UtcNow;

        return await _repository.UpdateAsync(template, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteTemplateAsync(int id, int tenantId, string deletedBy, CancellationToken cancellationToken = default)
    {
        // Check authorization first
        var template = await GetTemplateAsync(id, tenantId, cancellationToken);
        if (template == null || template.TenantId != tenantId)
        {
            return false;
        }

        return await _repository.DeleteAsync(id, deletedBy, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TemplateRenderResult> RenderTemplateAsync(
        int templateId,
        int tenantId,
        Dictionary<string, string> variables,
        string usedBy,
        long? documentId = null,
        CancellationToken cancellationToken = default)
    {
        var result = new TemplateRenderResult();

        // Get template with authorization check
        var template = await GetTemplateAsync(templateId, tenantId, cancellationToken);
        if (template == null)
        {
            result.IsSuccess = false;
            result.Errors.Add("Template not found or access denied");
            return result;
        }

        // Parse template variables
        var templateVariables = ParseTemplateVariables(template.Variables);
        var variableDict = templateVariables.ToDictionary(v => v.Name, v => v);

        // Start with template content
        var renderedContent = template.Content;

        // Extract all variable placeholders from content
        var matches = VariablePattern.Matches(renderedContent);
        var usedVariables = new HashSet<string>();

        foreach (Match match in matches)
        {
            var variableName = match.Groups[1].Value;
            usedVariables.Add(variableName);

            // Check if variable is defined in template
            if (!variableDict.ContainsKey(variableName))
            {
                result.Warnings.Add($"Variable '{variableName}' found in content but not defined in template variables");
            }
        }

        // Process each variable
        foreach (var variableName in usedVariables)
        {
            var placeholder = $"{{{{{variableName}}}}}";
            string? valueToUse = null;

            // Check if value provided
            if (variables.TryGetValue(variableName, out var providedValue))
            {
                valueToUse = providedValue;
            }
            // Check for default value
            else if (variableDict.TryGetValue(variableName, out var varDef) && !string.IsNullOrEmpty(varDef.DefaultValue))
            {
                valueToUse = varDef.DefaultValue;
                result.Warnings.Add($"Using default value for variable '{variableName}'");
            }
            // Check if required
            else if (variableDict.TryGetValue(variableName, out var varDefRequired) && varDefRequired.IsRequired)
            {
                result.Errors.Add($"Required variable '{variableName}' is missing");
                result.MissingVariables.Add(variableName);
                result.IsSuccess = false;
                continue;
            }
            else
            {
                result.Warnings.Add($"Optional variable '{variableName}' not provided and has no default value");
                result.MissingVariables.Add(variableName);
                valueToUse = string.Empty;
            }

            // Perform substitution
            if (valueToUse != null)
            {
                renderedContent = renderedContent.Replace(placeholder, valueToUse);
                result.SubstitutedVariables[variableName] = valueToUse;
            }
        }

        result.RenderedContent = renderedContent;

        // Log usage
        var usageLog = new TemplateUsageLog
        {
            TemplateId = templateId,
            TenantId = tenantId,
            DocumentId = documentId,
            UsedBy = usedBy,
            Status = result.IsSuccess ? "Success" : result.Errors.Count > 0 ? "Failed" : "PartialSuccess",
            ErrorMessage = result.Errors.Count > 0 ? string.Join("; ", result.Errors) : null,
            VariablesUsed = JsonSerializer.Serialize(variables)
        };

        await _repository.LogUsageAsync(usageLog, cancellationToken);

        return result;
    }

    /// <inheritdoc />
    public List<TemplateVariable> ParseTemplateVariables(string? variablesJson)
    {
        if (string.IsNullOrWhiteSpace(variablesJson))
        {
            return new List<TemplateVariable>();
        }

        try
        {
            var variables = JsonSerializer.Deserialize<List<TemplateVariable>>(variablesJson);
            return variables ?? new List<TemplateVariable>();
        }
        catch (JsonException)
        {
            return new List<TemplateVariable>();
        }
    }

    /// <inheritdoc />
    public List<string> ValidateTemplate(string content, List<TemplateVariable> variables)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(content))
        {
            errors.Add("Template content cannot be empty");
            return errors;
        }

        // Extract all variable placeholders
        var matches = VariablePattern.Matches(content);
        var contentVariables = new HashSet<string>();

        foreach (Match match in matches)
        {
            contentVariables.Add(match.Groups[1].Value);
        }

        // Check for undefined variables in content
        var definedVariables = new HashSet<string>(variables.Select(v => v.Name));
        foreach (var contentVar in contentVariables)
        {
            if (!definedVariables.Contains(contentVar))
            {
                errors.Add($"Variable '{contentVar}' used in content but not defined in variables");
            }
        }

        // Check for duplicate variable names
        var variableNames = variables.Select(v => v.Name).ToList();
        var duplicates = variableNames.GroupBy(n => n).Where(g => g.Count() > 1).Select(g => g.Key);
        foreach (var duplicate in duplicates)
        {
            errors.Add($"Duplicate variable name: '{duplicate}'");
        }

        // Validate variable definitions
        foreach (var variable in variables)
        {
            if (string.IsNullOrWhiteSpace(variable.Name))
            {
                errors.Add("Variable name cannot be empty");
            }

            if (string.IsNullOrWhiteSpace(variable.Label))
            {
                errors.Add($"Variable '{variable.Name}' must have a label");
            }

            if (!string.IsNullOrEmpty(variable.ValidationPattern))
            {
                try
                {
                    _ = new Regex(variable.ValidationPattern);
                }
                catch (ArgumentException)
                {
                    errors.Add($"Variable '{variable.Name}' has invalid validation pattern");
                }
            }
        }

        return errors;
    }

    /// <summary>
    /// Regex pattern for matching variable placeholders in template content.
    /// Matches {{variableName}} syntax.
    /// </summary>
    [GeneratedRegex(@"\{\{(\w+)\}\}", RegexOptions.Compiled)]
    private static partial Regex VariablePlaceholderRegex();
}
