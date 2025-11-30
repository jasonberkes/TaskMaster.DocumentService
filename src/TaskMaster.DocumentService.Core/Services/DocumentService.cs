using Microsoft.Extensions.Logging;
using TaskMaster.DocumentService.Core.DTOs;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Interfaces;

namespace TaskMaster.DocumentService.Core.Services;

/// <summary>
/// Service implementation for document operations.
/// </summary>
public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly ILogger<DocumentService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentService"/> class.
    /// </summary>
    public DocumentService(IDocumentRepository documentRepository, ILogger<DocumentService> logger)
    {
        _documentRepository = documentRepository;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<DocumentDto?> GetDocumentAsync(Guid id, Guid tenantId)
    {
        try
        {
            var document = await _documentRepository.GetByIdAsync(id, tenantId);
            return document != null ? MapToDto(document) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document {DocumentId} for tenant {TenantId}", id, tenantId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DocumentDto>> GetTenantDocumentsAsync(Guid tenantId)
    {
        try
        {
            var documents = await _documentRepository.GetByTenantAsync(tenantId);
            return documents.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving documents for tenant {TenantId}", tenantId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<DocumentDto> CreateDocumentAsync(CreateDocumentDto createDto, Guid tenantId, string userId)
    {
        try
        {
            var document = new Document
            {
                TenantId = tenantId,
                Name = createDto.Name,
                ContentType = createDto.ContentType,
                Size = createDto.Size,
                BlobPath = createDto.BlobPath,
                UploadedBy = userId
            };

            var createdDocument = await _documentRepository.CreateAsync(document);

            _logger.LogInformation("Document {DocumentId} created for tenant {TenantId} by user {UserId}",
                createdDocument.Id, tenantId, userId);

            return MapToDto(createdDocument);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating document for tenant {TenantId}", tenantId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<DocumentDto?> UpdateDocumentAsync(Guid id, UpdateDocumentDto updateDto, Guid tenantId)
    {
        try
        {
            var document = await _documentRepository.GetByIdAsync(id, tenantId);

            if (document == null)
            {
                _logger.LogWarning("Document {DocumentId} not found for tenant {TenantId}", id, tenantId);
                return null;
            }

            if (!string.IsNullOrWhiteSpace(updateDto.Name))
            {
                document.Name = updateDto.Name;
            }

            var updatedDocument = await _documentRepository.UpdateAsync(document);

            _logger.LogInformation("Document {DocumentId} updated for tenant {TenantId}", id, tenantId);

            return MapToDto(updatedDocument);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document {DocumentId} for tenant {TenantId}", id, tenantId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteDocumentAsync(Guid id, Guid tenantId)
    {
        try
        {
            var result = await _documentRepository.DeleteAsync(id, tenantId);

            if (result)
            {
                _logger.LogInformation("Document {DocumentId} deleted for tenant {TenantId}", id, tenantId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {DocumentId} for tenant {TenantId}", id, tenantId);
            throw;
        }
    }

    private static DocumentDto MapToDto(Document document)
    {
        return new DocumentDto
        {
            Id = document.Id,
            TenantId = document.TenantId,
            Name = document.Name,
            ContentType = document.ContentType,
            Size = document.Size,
            UploadedAt = document.UploadedAt,
            UploadedBy = document.UploadedBy
        };
    }
}
