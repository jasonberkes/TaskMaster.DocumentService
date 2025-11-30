namespace TaskMaster.DocumentService.SDK.Interfaces;

/// <summary>
/// Main client interface for the Document Service SDK.
/// </summary>
public interface IDocumentServiceClient : IDisposable
{
    /// <summary>
    /// Gets the documents client for document operations.
    /// </summary>
    IDocumentsClient Documents { get; }

    /// <summary>
    /// Gets the document types client for document type operations.
    /// </summary>
    IDocumentTypesClient DocumentTypes { get; }

    /// <summary>
    /// Gets the tenants client for tenant operations.
    /// </summary>
    ITenantsClient Tenants { get; }
}
