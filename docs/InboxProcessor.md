# Document Service: Inbox Processor Background Service

## Overview

The Inbox Processor is a background service that implements a **dump-and-index pattern** for automated document ingestion. It continuously monitors an Azure Blob Storage "inbox" container and automatically processes any files dropped into it, creating document records in the system without requiring explicit API calls.

## Architecture

### Components

1. **InboxProcessorBackgroundService** (`BackgroundServices/InboxProcessorBackgroundService.cs`)
   - Hosted service that runs continuously in the background
   - Polls the inbox container at regular intervals
   - Creates service scopes to ensure proper dependency injection

2. **InboxProcessorService** (`Services/InboxProcessorService.cs`)
   - Core processing logic for handling inbox files
   - Extracts metadata from blob properties and file paths
   - Creates documents via the DocumentService
   - Moves processed/failed files to appropriate containers

3. **InboxProcessorOptions** (`Configuration/InboxProcessorOptions.cs`)
   - Configuration settings for the processor
   - Controls polling interval, batch size, defaults, etc.

4. **InboxFileMetadata** (`Models/InboxFileMetadata.cs`)
   - Represents metadata extracted from inbox files
   - Supports multiple metadata sources (blob metadata, folder structure, etc.)

### Flow Diagram

```
┌─────────────────┐
│  Blob Storage   │
│  Inbox Container│
└────────┬────────┘
         │ Drop Files
         ▼
┌─────────────────────────────────┐
│ InboxProcessorBackgroundService │
│  (Polls every N seconds)        │
└────────┬────────────────────────┘
         │ GetBlobsAsync
         ▼
┌─────────────────────────────┐
│   InboxProcessorService     │
│  - Extract Metadata         │
│  - Download Content         │
│  - Create Document          │
│  - Move to Processed/Failed │
└─────┬────────────────┬──────┘
      │                │
      ▼                ▼
┌──────────┐    ┌────────────┐
│Processed │    │  Failed    │
│Container │    │ Container  │
└──────────┘    └────────────┘
```

## Configuration

Add the following to your `appsettings.json`:

```json
{
  "InboxProcessor": {
    "Enabled": true,
    "InboxContainerName": "inbox",
    "ProcessedContainerName": "processed",
    "FailedContainerName": "failed",
    "PollingIntervalSeconds": 30,
    "BatchSize": 10,
    "DefaultTenantId": 1,
    "DefaultDocumentTypeId": 1,
    "SystemUser": "InboxProcessor"
  }
}
```

### Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Enabled` | bool | true | Enable/disable the inbox processor |
| `InboxContainerName` | string | "inbox" | Name of the container to monitor |
| `ProcessedContainerName` | string | "processed" | Where to move successfully processed files |
| `FailedContainerName` | string | "failed" | Where to move files that failed processing |
| `PollingIntervalSeconds` | int | 30 | How often to check for new files (seconds) |
| `BatchSize` | int | 10 | Maximum files to process per cycle |
| `DefaultTenantId` | int | 1 | Default tenant ID if not specified in metadata |
| `DefaultDocumentTypeId` | int | 1 | Default document type if not specified |
| `SystemUser` | string | "InboxProcessor" | User name for document creation |

## Usage

### Method 1: Simple File Drop

Drop a file directly into the inbox container:

```
inbox/
  └── my-document.pdf
```

The processor will:
- Use the filename as the document title
- Assign default tenant and document type
- Create the document automatically

### Method 2: Folder-Based Tenant Routing

Organize files by tenant using folder structure:

```
inbox/
  ├── tenant-1/
  │   └── document1.pdf
  ├── tenant-5/
  │   └── document2.docx
  └── tenant-10/
      └── document3.txt
```

The processor will extract the tenant ID from the folder name (`tenant-{id}`).

### Method 3: Blob Metadata Tags

Provide detailed metadata using Azure Blob metadata tags:

```csharp
var blobClient = containerClient.GetBlobClient("document.pdf");
var metadata = new Dictionary<string, string>
{
    ["TenantId"] = "5",
    ["DocumentTypeId"] = "2",
    ["Title"] = "Q4 Financial Report",
    ["Description"] = "Quarterly financial report for Q4 2024",
    ["Tags"] = "{\"department\":\"finance\",\"year\":\"2024\"}",
    ["Metadata"] = "{\"quarter\":\"Q4\",\"confidential\":true}"
};

await blobClient.UploadAsync(stream, new BlobUploadOptions
{
    Metadata = metadata
});
```

Supported metadata tags:
- `TenantId` - Integer tenant identifier
- `DocumentTypeId` - Integer document type identifier
- `Title` - Document title
- `Description` - Document description
- `Tags` - JSON string of tags
- `Metadata` - JSON string of additional metadata

## Processing Behavior

### Success Flow

1. File is detected in inbox container
2. Metadata is extracted (blob metadata, folder structure, filename)
3. File content is downloaded
4. Document is created via DocumentService
5. File is moved to the `processed` container with timestamp prefix
6. Processing metadata is added to the moved file

### Failure Flow

1. File processing encounters an error
2. Error details are logged
3. File is moved to the `failed` container
4. Error information is added to blob metadata:
   - `ErrorMessage` - The exception message
   - `ErrorTime` - ISO 8601 timestamp of failure
   - `ProcessedBy` - System user that attempted processing

### Processed File Naming

Processed files are renamed with a timestamp prefix to prevent conflicts:

```
Original: tenant-5/document.pdf
Processed: 20241130142530_tenant-5_document.pdf
```

## Monitoring and Logging

The inbox processor logs at various levels:

### Information Level
- Service start/stop events
- Processing cycle summaries
- Successful document creation

### Debug Level
- Polling cycle start/complete
- Metadata extraction details
- Empty inbox checks

### Error Level
- File processing failures
- Blob operation errors
- Exception details

### Example Log Output

```
[Information] Inbox processor background service starting. Polling interval: 30 seconds
[Information] Starting inbox file processing
[Information] Found 3 files in inbox to process
[Information] Processing file: tenant-5/report.pdf
[Debug] Extracted metadata for tenant-5/report.pdf: TenantId=5, DocumentTypeId=1, Title=report
[Information] Successfully created document 123 from file tenant-5/report.pdf
[Information] Moved file tenant-5/report.pdf to processed/20241130142530_tenant-5_report.pdf
[Information] Inbox processing completed. Processed 3 of 3 files
```

## Service Registration

The inbox processor is registered in `Program.cs`:

```csharp
using TaskMaster.DocumentService.Processing.Extensions;

// Add Document Service Processing layer (Inbox Processor Background Service)
builder.Services.AddDocumentServiceProcessing(builder.Configuration);
```

This registers:
- `IInboxProcessorService` → `InboxProcessorService`
- `InboxProcessorBackgroundService` as a hosted service
- Configuration binding for `InboxProcessorOptions`

## Performance Considerations

### Batch Processing
- The `BatchSize` setting limits how many files are processed per cycle
- This prevents memory issues with large numbers of files
- Unprocessed files will be picked up in the next cycle

### Polling Interval
- Shorter intervals = faster processing, higher CPU usage
- Longer intervals = lower resource usage, slower processing
- Recommended: 10-30 seconds for development, 30-60 seconds for production

### Scalability
- The service creates a new scope per processing cycle
- DbContext and other scoped services are properly disposed
- Safe to run in Azure Container Apps with auto-scaling

## Error Handling

The service implements comprehensive error handling:

1. **Per-File Error Handling**
   - Exceptions during file processing don't stop the batch
   - Failed files are moved to the failed container
   - Processing continues with remaining files

2. **Cycle-Level Error Handling**
   - Exceptions during polling don't crash the service
   - Service waits for the polling interval and retries
   - Logs errors at Error level for monitoring

3. **Graceful Shutdown**
   - Cancellation token is respected
   - In-progress operations are allowed to complete
   - Clean service stop on application shutdown

## Testing

Comprehensive unit tests are provided in `TaskMaster.DocumentService.Processing.Tests`:

- **InboxProcessorServiceTests**: 10+ test cases covering:
  - Constructor validation
  - Disabled processor behavior
  - Empty inbox handling
  - Single and multiple file processing
  - Batch size limits
  - Tenant folder structure parsing
  - Error scenarios

- **InboxProcessorBackgroundServiceTests**: 7+ test cases covering:
  - Constructor validation
  - Enabled/disabled behavior
  - Background execution
  - Cancellation handling
  - Exception recovery

Run tests:
```bash
dotnet test
```

## Troubleshooting

### Files not being processed

1. Check if the service is enabled:
   ```json
   "InboxProcessor": { "Enabled": true }
   ```

2. Verify blob storage connection:
   ```json
   "ConnectionStrings": {
     "BlobStorage": "your-connection-string"
   }
   ```

3. Check service logs for errors

### Files moved to failed container

1. Check blob metadata on failed file for error details
2. Review application logs for full exception stack traces
3. Common issues:
   - Invalid tenant or document type IDs
   - Missing required metadata
   - Unsupported file formats

### High resource usage

1. Increase polling interval
2. Reduce batch size
3. Monitor blob storage costs (list operations)

## Future Enhancements

Potential improvements for future iterations:

- Event Grid integration for real-time processing (eliminate polling)
- Parallel processing of files within a batch
- Retry logic with exponential backoff
- Dead letter queue for repeatedly failing files
- Metrics and telemetry (Application Insights)
- Support for subfolders and complex routing rules
- Automatic document type detection based on file content
- OCR and text extraction for images
- Virus scanning integration

## References

- Work Item: #2295
- Azure Blob Storage: https://docs.microsoft.com/azure/storage/blobs/
- Background Services: https://docs.microsoft.com/aspnet/core/fundamentals/host/hosted-services
