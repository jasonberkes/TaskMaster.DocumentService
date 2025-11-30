using TaskMaster.DocumentService.SDK.DTOs;
using TaskMaster.DocumentService.SDK.Interfaces;

namespace TaskMaster.DocumentService.SDK.Clients;

/// <summary>
/// Client implementation for document type operations.
/// </summary>
public class DocumentTypesClient : BaseClient, IDocumentTypesClient
{
    private const string BaseEndpoint = "api/document-types";

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentTypesClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use for API calls.</param>
    public DocumentTypesClient(HttpClient httpClient) : base(httpClient)
    {
    }

    /// <inheritdoc/>
    public async Task<DocumentTypeDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await GetAsync<DocumentTypeDto>($"{BaseEndpoint}/{id}", cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<DocumentTypeDto?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await GetAsync<DocumentTypeDto>($"{BaseEndpoint}/name/{Uri.EscapeDataString(name)}", cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DocumentTypeDto>> GetAllAsync(bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        var result = await GetAsync<IEnumerable<DocumentTypeDto>>($"{BaseEndpoint}?activeOnly={activeOnly}", cancellationToken);
        return result ?? Array.Empty<DocumentTypeDto>();
    }

    /// <inheritdoc/>
    public async Task<DocumentTypeDto> CreateAsync(DocumentTypeDto documentType, CancellationToken cancellationToken = default)
    {
        return await PostAsync<DocumentTypeDto>(BaseEndpoint, documentType, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<DocumentTypeDto> UpdateAsync(int id, DocumentTypeDto documentType, CancellationToken cancellationToken = default)
    {
        return await PutAsync<DocumentTypeDto>($"{BaseEndpoint}/{id}", documentType, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await DeleteAsync($"{BaseEndpoint}/{id}", cancellationToken);
    }
}
