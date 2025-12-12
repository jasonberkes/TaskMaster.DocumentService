# TaskMaster DocumentService Client SDK

.NET client SDK for communicating with the TaskMaster DocumentService API.

## Installation

```bash
dotnet add package TaskMaster.DocumentService.Client
```

## Quick Start

### Configuration

Add to your `appsettings.json`:

```json
{
  "DocumentService": {
    "BaseUrl": "https://tm-documentservice-prod-eus2.thankfulsand-8986c25c.eastus2.azurecontainerapps.io",
    "ApiKey": "your-api-key-here",
    "TimeoutSeconds": 30
  }
}
```

### Registration

```csharp
// In Program.cs or Startup.cs
services.AddDocumentServiceClient(configuration.GetSection("DocumentService"));
```

### Usage

```csharp
public class MyService
{
    private readonly IDocumentServiceClient _documentService;
    
    public MyService(IDocumentServiceClient documentService)
    {
        _documentService = documentService;
    }
    
    public async Task<IEnumerable<DocumentDto>> GetDocumentsAsync(int tenantId)
    {
        return await _documentService.Documents.GetByTenantIdAsync(tenantId);
    }
    
    public async Task<DocumentDto> UploadDocumentAsync(int tenantId, Stream content, string fileName)
    {
        var request = new CreateDocumentRequest
        {
            TenantId = tenantId,
            Title = fileName,
            OriginalFileName = fileName,
            Content = content
        };
        
        return await _documentService.Documents.CreateAsync(request);
    }
}
```

## Available Clients

| Client | Description |
|--------|-------------|
| `Documents` | Upload, download, search, archive, and manage documents |
| `DocumentTypes` | Query available document types |
| `Tenants` | Tenant information and hierarchy |

## Features

- **Typed Clients**: Strongly-typed interfaces for all API operations
- **Dependency Injection**: Easy integration with Microsoft.Extensions.DependencyInjection
- **Configurable**: Support for configuration binding and options pattern
- **Resilient**: Built-in timeout and retry support via HttpClient

## License

MIT
