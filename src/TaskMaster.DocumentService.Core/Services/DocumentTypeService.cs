using Microsoft.Extensions.Logging;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Interfaces;

namespace TaskMaster.DocumentService.Core.Services;

/// <summary>
/// Service implementation for document type management operations.
/// </summary>
public class DocumentTypeService : IDocumentTypeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DocumentTypeService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentTypeService"/> class.
    /// </summary>
    /// <param name="unitOfWork">The unit of work.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public DocumentTypeService(
        IUnitOfWork unitOfWork,
        ILogger<DocumentTypeService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<DocumentType?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ArgumentException("Document type ID must be greater than zero.", nameof(id));

        _logger.LogDebug("Getting document type by ID: {DocumentTypeId}", id);

        try
        {
            var documentType = await _unitOfWork.DocumentTypes.GetByIdAsync(id, cancellationToken);

            if (documentType == null)
            {
                _logger.LogWarning("Document type with ID {DocumentTypeId} not found", id);
            }

            return documentType;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document type by ID: {DocumentTypeId}", id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<DocumentType?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Document type name cannot be null or empty.", nameof(name));

        _logger.LogDebug("Getting document type by name: {Name}", name);

        try
        {
            var documentType = await _unitOfWork.DocumentTypes.GetByNameAsync(name, cancellationToken);

            if (documentType == null)
            {
                _logger.LogWarning("Document type with name {Name} not found", name);
            }

            return documentType;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document type by name: {Name}", name);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DocumentType>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all document types");

        try
        {
            var documentTypes = await _unitOfWork.DocumentTypes.GetAllAsync(cancellationToken);
            _logger.LogInformation("Retrieved {Count} document types", documentTypes.Count());
            return documentTypes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all document types");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DocumentType>> GetActiveDocumentTypesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting active document types");

        try
        {
            var documentTypes = await _unitOfWork.DocumentTypes.GetActiveDocumentTypesAsync(cancellationToken);
            _logger.LogInformation("Retrieved {Count} active document types", documentTypes.Count());
            return documentTypes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active document types");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DocumentType>> GetIndexableTypesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting indexable document types");

        try
        {
            var documentTypes = await _unitOfWork.DocumentTypes.GetIndexableTypesAsync(cancellationToken);
            _logger.LogInformation("Retrieved {Count} indexable document types", documentTypes.Count());
            return documentTypes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting indexable document types");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DocumentType>> GetTypesWithExtensionTablesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting document types with extension tables");

        try
        {
            var documentTypes = await _unitOfWork.DocumentTypes.GetTypesWithExtensionTablesAsync(cancellationToken);
            _logger.LogInformation("Retrieved {Count} document types with extension tables", documentTypes.Count());
            return documentTypes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document types with extension tables");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<DocumentType> CreateAsync(DocumentType documentType, CancellationToken cancellationToken = default)
    {
        if (documentType == null)
            throw new ArgumentNullException(nameof(documentType));

        if (string.IsNullOrWhiteSpace(documentType.Name))
            throw new ArgumentException("Document type name cannot be null or empty.", nameof(documentType));

        if (string.IsNullOrWhiteSpace(documentType.DisplayName))
            throw new ArgumentException("Document type display name cannot be null or empty.", nameof(documentType));

        _logger.LogInformation("Creating new document type: {Name}", documentType.Name);

        try
        {
            // Check if name already exists
            var existingDocumentType = await _unitOfWork.DocumentTypes.GetByNameAsync(documentType.Name, cancellationToken);
            if (existingDocumentType != null)
            {
                _logger.LogWarning("Document type with name {Name} already exists", documentType.Name);
                throw new InvalidOperationException($"A document type with name '{documentType.Name}' already exists.");
            }

            // Validate extension table configuration
            if (documentType.HasExtensionTable && string.IsNullOrWhiteSpace(documentType.ExtensionTableName))
            {
                _logger.LogWarning("Document type {Name} has extension table enabled but no table name specified", documentType.Name);
                throw new ArgumentException("Extension table name is required when HasExtensionTable is true.", nameof(documentType));
            }

            if (!documentType.HasExtensionTable && !string.IsNullOrWhiteSpace(documentType.ExtensionTableName))
            {
                _logger.LogWarning("Document type {Name} has extension table name but HasExtensionTable is false", documentType.Name);
                documentType.ExtensionTableName = null;
            }

            // Set timestamps
            documentType.CreatedAt = DateTime.UtcNow;

            var createdDocumentType = await _unitOfWork.DocumentTypes.AddAsync(documentType, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully created document type: {Name} with ID: {DocumentTypeId}",
                createdDocumentType.Name, createdDocumentType.Id);

            return createdDocumentType;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating document type: {Name}", documentType.Name);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<DocumentType> UpdateAsync(DocumentType documentType, CancellationToken cancellationToken = default)
    {
        if (documentType == null)
            throw new ArgumentNullException(nameof(documentType));

        if (documentType.Id <= 0)
            throw new ArgumentException("Document type ID must be greater than zero.", nameof(documentType));

        if (string.IsNullOrWhiteSpace(documentType.Name))
            throw new ArgumentException("Document type name cannot be null or empty.", nameof(documentType));

        if (string.IsNullOrWhiteSpace(documentType.DisplayName))
            throw new ArgumentException("Document type display name cannot be null or empty.", nameof(documentType));

        _logger.LogInformation("Updating document type: {DocumentTypeId}", documentType.Id);

        try
        {
            // Check if document type exists
            var existingDocumentType = await _unitOfWork.DocumentTypes.GetByIdAsync(documentType.Id, cancellationToken);
            if (existingDocumentType == null)
            {
                _logger.LogWarning("Document type with ID {DocumentTypeId} not found", documentType.Id);
                throw new InvalidOperationException($"Document type with ID {documentType.Id} does not exist.");
            }

            // Check if name conflicts with another document type
            var documentTypeWithName = await _unitOfWork.DocumentTypes.GetByNameAsync(documentType.Name, cancellationToken);
            if (documentTypeWithName != null && documentTypeWithName.Id != documentType.Id)
            {
                _logger.LogWarning("Document type with name {Name} already exists", documentType.Name);
                throw new InvalidOperationException($"A document type with name '{documentType.Name}' already exists.");
            }

            // Validate extension table configuration
            if (documentType.HasExtensionTable && string.IsNullOrWhiteSpace(documentType.ExtensionTableName))
            {
                _logger.LogWarning("Document type {Name} has extension table enabled but no table name specified", documentType.Name);
                throw new ArgumentException("Extension table name is required when HasExtensionTable is true.", nameof(documentType));
            }

            if (!documentType.HasExtensionTable && !string.IsNullOrWhiteSpace(documentType.ExtensionTableName))
            {
                _logger.LogWarning("Document type {Name} has extension table name but HasExtensionTable is false", documentType.Name);
                documentType.ExtensionTableName = null;
            }

            // Preserve creation timestamp
            documentType.CreatedAt = existingDocumentType.CreatedAt;

            _unitOfWork.DocumentTypes.Update(documentType);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            var updatedDocumentType = documentType;

            _logger.LogInformation("Successfully updated document type: {DocumentTypeId}", updatedDocumentType.Id);

            return updatedDocumentType;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document type: {DocumentTypeId}", documentType.Id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ArgumentException("Document type ID must be greater than zero.", nameof(id));

        _logger.LogInformation("Deleting document type: {DocumentTypeId}", id);

        try
        {
            // Check if document type exists
            var documentType = await _unitOfWork.DocumentTypes.GetByIdAsync(id, cancellationToken);
            if (documentType == null)
            {
                _logger.LogWarning("Document type with ID {DocumentTypeId} not found", id);
                return false;
            }

            // Check if document type is in use by documents
            var documents = documentType.Documents;
            if (documents != null && documents.Any())
            {
                _logger.LogWarning("Cannot delete document type {DocumentTypeId} because it is used by {Count} documents",
                    id, documents.Count);
                throw new InvalidOperationException($"Cannot delete document type {id} because it is used by {documents.Count} document(s). Remove or reassign documents first.");
            }

            _unitOfWork.DocumentTypes.Remove(documentType);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully deleted document type: {DocumentTypeId}", id);

            return true;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document type: {DocumentTypeId}", id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Document type name cannot be null or empty.", nameof(name));

        _logger.LogDebug("Checking if document type exists with name: {Name}", name);

        try
        {
            var documentType = await _unitOfWork.DocumentTypes.GetByNameAsync(name, cancellationToken);
            return documentType != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking document type existence by name: {Name}", name);
            throw;
        }
    }
}
