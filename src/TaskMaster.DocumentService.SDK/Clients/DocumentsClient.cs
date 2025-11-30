using TaskMaster.DocumentService.SDK.DTOs;
using TaskMaster.DocumentService.SDK.Interfaces;

namespace TaskMaster.DocumentService.SDK.Clients;

/// <summary>
/// Client implementation for document operations.
/// </summary>
public class DocumentsClient : BaseClient, IDocumentsClient
{
    private const string BaseEndpoint = "api/documents";

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentsClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use for API calls.</param>
    public DocumentsClient(HttpClient httpClient) : base(httpClient)
    {
    }

    /// <inheritdoc/>
    public async Task<DocumentDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await GetAsync<DocumentDto>($"{BaseEndpoint}/{id}", cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DocumentDto>> GetByTenantIdAsync(int tenantId, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var endpoint = $"{BaseEndpoint}/tenant/{tenantId}?includeDeleted={includeDeleted}";
        var result = await GetAsync<IEnumerable<DocumentDto>>(endpoint, cancellationToken);
        return result ?? Array.Empty<DocumentDto>();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DocumentDto>> GetByDocumentTypeIdAsync(int documentTypeId, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var endpoint = $"{BaseEndpoint}/type/{documentTypeId}?includeDeleted={includeDeleted}";
        var result = await GetAsync<IEnumerable<DocumentDto>>(endpoint, cancellationToken);
        return result ?? Array.Empty<DocumentDto>();
    }

    /// <inheritdoc/>
    public async Task<DocumentDto?> GetCurrentVersionAsync(long parentDocumentId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<DocumentDto>($"{BaseEndpoint}/{parentDocumentId}/current-version", cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DocumentDto>> GetVersionsAsync(long parentDocumentId, CancellationToken cancellationToken = default)
    {
        var result = await GetAsync<IEnumerable<DocumentDto>>($"{BaseEndpoint}/{parentDocumentId}/versions", cancellationToken);
        return result ?? Array.Empty<DocumentDto>();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DocumentDto>> GetArchivedAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        var result = await GetAsync<IEnumerable<DocumentDto>>($"{BaseEndpoint}/tenant/{tenantId}/archived", cancellationToken);
        return result ?? Array.Empty<DocumentDto>();
    }

    /// <inheritdoc/>
    public async Task<DocumentDto> CreateAsync(CreateDocumentRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<DocumentDto>(BaseEndpoint, request, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<DocumentDto> UpdateAsync(long id, UpdateDocumentRequest request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<DocumentDto>($"{BaseEndpoint}/{id}", request, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(long id, string deletedBy, string? deletedReason = null, CancellationToken cancellationToken = default)
    {
        var request = new
        {
            DeletedBy = deletedBy,
            DeletedReason = deletedReason
        };
        await PostAsync($"{BaseEndpoint}/{id}/delete", request, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task RestoreAsync(long id, CancellationToken cancellationToken = default)
    {
        await PostAsync($"{BaseEndpoint}/{id}/restore", new { }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task ArchiveAsync(long id, CancellationToken cancellationToken = default)
    {
        await PostAsync($"{BaseEndpoint}/{id}/archive", new { }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<DocumentDto> UploadAsync(CreateDocumentRequest request, Stream content, CancellationToken cancellationToken = default)
    {
        var additionalData = new Dictionary<string, string>
        {
            ["tenantId"] = request.TenantId.ToString(),
            ["documentTypeId"] = request.DocumentTypeId.ToString(),
            ["title"] = request.Title,
            ["blobPath"] = request.BlobPath
        };

        if (!string.IsNullOrEmpty(request.Description))
            additionalData["description"] = request.Description;
        if (!string.IsNullOrEmpty(request.ContentHash))
            additionalData["contentHash"] = request.ContentHash;
        if (request.FileSizeBytes.HasValue)
            additionalData["fileSizeBytes"] = request.FileSizeBytes.Value.ToString();
        if (!string.IsNullOrEmpty(request.MimeType))
            additionalData["mimeType"] = request.MimeType;
        if (!string.IsNullOrEmpty(request.OriginalFileName))
            additionalData["originalFileName"] = request.OriginalFileName;
        if (!string.IsNullOrEmpty(request.Metadata))
            additionalData["metadata"] = request.Metadata;
        if (!string.IsNullOrEmpty(request.Tags))
            additionalData["tags"] = request.Tags;
        if (!string.IsNullOrEmpty(request.CreatedBy))
            additionalData["createdBy"] = request.CreatedBy;

        return await UploadStreamAsync<DocumentDto>(
            $"{BaseEndpoint}/upload",
            content,
            request.OriginalFileName ?? "file",
            additionalData,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Stream> DownloadAsync(long id, CancellationToken cancellationToken = default)
    {
        return await DownloadStreamAsync($"{BaseEndpoint}/{id}/download", cancellationToken);
    }
}
