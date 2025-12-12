using TaskMaster.DocumentService.SDK.Interfaces;

namespace TaskMaster.DocumentService.SDK.Clients;

/// <summary>
/// Main client for the Document Service SDK that provides access to all service clients.
/// </summary>
public class DocumentServiceClient : IDocumentServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly Lazy<IDocumentsClient> _documentsClient;
    private readonly Lazy<IDocumentTypesClient> _documentTypesClient;
    private readonly Lazy<ITenantsClient> _tenantsClient;
    private readonly Lazy<ISearchClient> _searchClient;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentServiceClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use for API calls.</param>
    public DocumentServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        _documentsClient = new Lazy<IDocumentsClient>(() => new DocumentsClient(_httpClient));
        _documentTypesClient = new Lazy<IDocumentTypesClient>(() => new DocumentTypesClient(_httpClient));
        _tenantsClient = new Lazy<ITenantsClient>(() => new TenantsClient(_httpClient));
        _searchClient = new Lazy<ISearchClient>(() => new SearchClient(_httpClient));
    }

    /// <inheritdoc/>
    public IDocumentsClient Documents => _documentsClient.Value;

    /// <inheritdoc/>
    public IDocumentTypesClient DocumentTypes => _documentTypesClient.Value;

    /// <inheritdoc/>
    public ITenantsClient Tenants => _tenantsClient.Value;

    /// <inheritdoc/>
    public ISearchClient Search => _searchClient.Value;

    /// <summary>
    /// Disposes the client and releases resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the client and releases resources.
    /// </summary>
    /// <param name="disposing">Whether to dispose managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _httpClient?.Dispose();
            }

            _disposed = true;
        }
    }
}
