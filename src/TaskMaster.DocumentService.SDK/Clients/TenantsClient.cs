using TaskMaster.DocumentService.SDK.DTOs;
using TaskMaster.DocumentService.SDK.Interfaces;

namespace TaskMaster.DocumentService.SDK.Clients;

/// <summary>
/// Client implementation for tenant operations.
/// </summary>
public class TenantsClient : BaseClient, ITenantsClient
{
    private const string BaseEndpoint = "api/tenants";

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantsClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use for API calls.</param>
    public TenantsClient(HttpClient httpClient) : base(httpClient)
    {
    }

    /// <inheritdoc/>
    public async Task<TenantDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await GetAsync<TenantDto>($"{BaseEndpoint}/{id}", cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<TenantDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await GetAsync<TenantDto>($"{BaseEndpoint}/slug/{Uri.EscapeDataString(slug)}", cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<TenantDto>> GetAllAsync(bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        var result = await GetAsync<IEnumerable<TenantDto>>($"{BaseEndpoint}?activeOnly={activeOnly}", cancellationToken);
        return result ?? Array.Empty<TenantDto>();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<TenantDto>> GetChildTenantsAsync(int parentTenantId, CancellationToken cancellationToken = default)
    {
        var result = await GetAsync<IEnumerable<TenantDto>>($"{BaseEndpoint}/{parentTenantId}/children", cancellationToken);
        return result ?? Array.Empty<TenantDto>();
    }

    /// <inheritdoc/>
    public async Task<TenantDto> CreateAsync(TenantDto tenant, CancellationToken cancellationToken = default)
    {
        return await PostAsync<TenantDto>(BaseEndpoint, tenant, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<TenantDto> UpdateAsync(int id, TenantDto tenant, CancellationToken cancellationToken = default)
    {
        return await PutAsync<TenantDto>($"{BaseEndpoint}/{id}", tenant, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await DeleteAsync($"{BaseEndpoint}/{id}", cancellationToken);
    }
}
