using System.Net.Http.Json;
using System.Text.Json;
using TaskMaster.DocumentService.SDK.DTOs;
using TaskMaster.DocumentService.SDK.Exceptions;
using TaskMaster.DocumentService.SDK.Interfaces;

namespace TaskMaster.DocumentService.SDK.Clients;

/// <summary>
/// Client implementation for document search operations.
/// </summary>
public class SearchClient : ISearchClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    public SearchClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <inheritdoc />
    public async Task<SearchResultDto> SearchAsync(SearchRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var response = await _httpClient.PostAsJsonAsync("api/search", request, _jsonOptions, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new DocumentServiceException(
                $"Search failed with status {response.StatusCode}: {errorContent}",
                "SearchError",
                (int)response.StatusCode);
        }

        var result = await response.Content.ReadFromJsonAsync<SearchResultDto>(_jsonOptions, cancellationToken);
        return result ?? new SearchResultDto();
    }

    /// <inheritdoc />
    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("api/search/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
