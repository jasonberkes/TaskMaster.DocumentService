using System.Net.Http.Json;
using System.Text.Json;
using TaskMaster.DocumentService.SDK.DTOs;
using TaskMaster.DocumentService.SDK.Exceptions;

namespace TaskMaster.DocumentService.SDK.Clients;

/// <summary>
/// Base client class with common HTTP operations.
/// </summary>
public abstract class BaseClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use for API calls.</param>
    protected BaseClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// Sends a GET request and returns the deserialized response.
    /// </summary>
    /// <typeparam name="T">The type of the response data.</typeparam>
    /// <param name="endpoint">The API endpoint.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The deserialized response.</returns>
    protected async Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(endpoint, cancellationToken);
        return await HandleResponseAsync<T>(response, cancellationToken);
    }

    /// <summary>
    /// Sends a POST request and returns the deserialized response.
    /// </summary>
    /// <typeparam name="T">The type of the response data.</typeparam>
    /// <param name="endpoint">The API endpoint.</param>
    /// <param name="data">The data to send in the request body.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The deserialized response.</returns>
    protected async Task<T> PostAsync<T>(string endpoint, object data, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(endpoint, data, _jsonOptions, cancellationToken);
        var result = await HandleResponseAsync<T>(response, cancellationToken);
        return result ?? throw new DocumentServiceException("Response data was null");
    }

    /// <summary>
    /// Sends a PUT request and returns the deserialized response.
    /// </summary>
    /// <typeparam name="T">The type of the response data.</typeparam>
    /// <param name="endpoint">The API endpoint.</param>
    /// <param name="data">The data to send in the request body.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The deserialized response.</returns>
    protected async Task<T> PutAsync<T>(string endpoint, object data, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync(endpoint, data, _jsonOptions, cancellationToken);
        var result = await HandleResponseAsync<T>(response, cancellationToken);
        return result ?? throw new DocumentServiceException("Response data was null");
    }

    /// <summary>
    /// Sends a DELETE request.
    /// </summary>
    /// <param name="endpoint">The API endpoint.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected async Task DeleteAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync(endpoint, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    /// <summary>
    /// Sends a POST request without expecting a response body.
    /// </summary>
    /// <param name="endpoint">The API endpoint.</param>
    /// <param name="data">The data to send in the request body.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected async Task PostAsync(string endpoint, object data, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(endpoint, data, _jsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    /// <summary>
    /// Downloads a stream from the specified endpoint.
    /// </summary>
    /// <param name="endpoint">The API endpoint.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response stream.</returns>
    protected async Task<Stream> DownloadStreamAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(endpoint, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

    /// <summary>
    /// Uploads a stream to the specified endpoint.
    /// </summary>
    /// <typeparam name="T">The type of the response data.</typeparam>
    /// <param name="endpoint">The API endpoint.</param>
    /// <param name="stream">The stream to upload.</param>
    /// <param name="fileName">The file name.</param>
    /// <param name="additionalData">Additional form data to include.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The deserialized response.</returns>
    protected async Task<T> UploadStreamAsync<T>(
        string endpoint,
        Stream stream,
        string fileName,
        Dictionary<string, string>? additionalData = null,
        CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(stream);
        content.Add(streamContent, "file", fileName);

        if (additionalData != null)
        {
            foreach (var kvp in additionalData)
            {
                content.Add(new StringContent(kvp.Value), kvp.Key);
            }
        }

        var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
        var result = await HandleResponseAsync<T>(response, cancellationToken);
        return result ?? throw new DocumentServiceException("Response data was null");
    }

    /// <summary>
    /// Handles the HTTP response and deserializes the content.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="response">The HTTP response message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The deserialized response.</returns>
    private async Task<T?> HandleResponseAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken);
        }

        await ThrowExceptionForErrorResponseAsync(response, cancellationToken);
        return default;
    }

    /// <summary>
    /// Ensures the HTTP response indicates success.
    /// </summary>
    /// <param name="response">The HTTP response message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            await ThrowExceptionForErrorResponseAsync(response, cancellationToken);
        }
    }

    /// <summary>
    /// Throws an appropriate exception based on the error response.
    /// </summary>
    /// <param name="response">The HTTP response message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task ThrowExceptionForErrorResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var statusCode = (int)response.StatusCode;
        string? errorMessage = null;
        string? errorCode = null;
        Dictionary<string, string[]>? validationErrors = null;

        try
        {
            var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(_jsonOptions, cancellationToken);
            errorMessage = errorResponse?.ErrorMessage;
            errorCode = errorResponse?.ErrorCode;
            validationErrors = errorResponse?.ValidationErrors;
        }
        catch
        {
            errorMessage = await response.Content.ReadAsStringAsync(cancellationToken);
        }

        errorMessage ??= $"HTTP request failed with status code {statusCode}";

        if (statusCode == 404)
        {
            throw new DocumentNotFoundException(errorMessage);
        }

        if (statusCode == 400 && validationErrors != null)
        {
            throw new ValidationException(errorMessage, validationErrors);
        }

        throw new DocumentServiceException(errorMessage, errorCode, statusCode);
    }
}
