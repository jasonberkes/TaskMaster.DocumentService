using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Interfaces;
using TaskMaster.DocumentService.Processing.Configuration;
using TaskMaster.DocumentService.Processing.Interfaces;

namespace TaskMaster.DocumentService.Processing.Services;

/// <summary>
/// Service for migrating code reviews from TaskMaster.Platform blob storage to the Document Service.
/// </summary>
public class CodeReviewMigrationService : ICodeReviewMigrationService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDocumentService _documentService;
    private readonly ILogger<CodeReviewMigrationService> _logger;
    private readonly CodeReviewMigrationOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeReviewMigrationService"/> class.
    /// </summary>
    /// <param name="blobServiceClient">The Azure Blob Service client.</param>
    /// <param name="unitOfWork">The unit of work for data access.</param>
    /// <param name="documentService">The document service.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The migration configuration options.</param>
    public CodeReviewMigrationService(
        BlobServiceClient blobServiceClient,
        IUnitOfWork unitOfWork,
        IDocumentService documentService,
        ILogger<CodeReviewMigrationService> logger,
        IOptions<CodeReviewMigrationOptions> options)
    {
        _blobServiceClient = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc/>
    public async Task<int> MigrateCodeReviewsAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogDebug("Code review migration is disabled");
            return 0;
        }

        var batchId = $"migration-{DateTime.UtcNow:yyyyMMddHHmmss}";

        try
        {
            _logger.LogInformation("Starting code review migration. Batch ID: {BatchId}", batchId);

            var sourceContainer = _blobServiceClient.GetBlobContainerClient(_options.SourceContainerName);

            // Check if source container exists
            if (!await sourceContainer.ExistsAsync(cancellationToken))
            {
                _logger.LogWarning("Source container '{ContainerName}' does not exist", _options.SourceContainerName);
                return 0;
            }

            // Ensure destination containers exist
            var migratedContainer = _blobServiceClient.GetBlobContainerClient(_options.MigratedContainerName);
            await migratedContainer.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            var failedContainer = _blobServiceClient.GetBlobContainerClient(_options.FailedMigrationContainerName);
            await failedContainer.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            // Get list of code review blobs to migrate
            var blobItems = new List<BlobItem>();
            await foreach (var blobItem in sourceContainer.GetBlobsAsync(cancellationToken: cancellationToken))
            {
                blobItems.Add(blobItem);

                if (blobItems.Count >= _options.BatchSize)
                {
                    break;
                }
            }

            if (blobItems.Count == 0)
            {
                _logger.LogInformation("No code review blobs found in source container");
                return 0;
            }

            _logger.LogInformation("Found {Count} code review blobs to migrate", blobItems.Count);

            var migratedCount = 0;

            foreach (var blobItem in blobItems)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Migration cancelled, migrated {Count} code reviews", migratedCount);
                    break;
                }

                var success = await MigrateCodeReviewBlobAsync(
                    sourceContainer,
                    migratedContainer,
                    failedContainer,
                    blobItem,
                    batchId,
                    cancellationToken);

                if (success)
                {
                    migratedCount++;
                }
            }

            _logger.LogInformation(
                "Code review migration completed. Batch ID: {BatchId}, Migrated: {MigratedCount} of {TotalCount}",
                batchId,
                migratedCount,
                blobItems.Count);

            return migratedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during code review migration. Batch ID: {BatchId}", batchId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<MigrationBatchStatus> GetMigrationStatusAsync(string batchId, CancellationToken cancellationToken = default)
    {
        var codeReviews = await _unitOfWork.CodeReviews.GetByMigrationBatchIdAsync(batchId, cancellationToken);
        var reviewList = codeReviews.ToList();

        return new MigrationBatchStatus
        {
            BatchId = batchId,
            TotalProcessed = reviewList.Count,
            SuccessCount = reviewList.Count, // All records in DB are successful
            FailureCount = 0, // Failures aren't stored in DB
            StartedAt = reviewList.MinBy(cr => cr.MigratedAt)?.MigratedAt,
            CompletedAt = reviewList.MaxBy(cr => cr.MigratedAt)?.MigratedAt
        };
    }

    private async Task<bool> MigrateCodeReviewBlobAsync(
        BlobContainerClient sourceContainer,
        BlobContainerClient migratedContainer,
        BlobContainerClient failedContainer,
        BlobItem blobItem,
        string batchId,
        CancellationToken cancellationToken)
    {
        var blobName = blobItem.Name;
        _logger.LogInformation("Migrating code review blob: {BlobName}", blobName);

        try
        {
            var sourceBlobClient = sourceContainer.GetBlobClient(blobName);

            // Get blob properties and metadata
            var properties = await sourceBlobClient.GetPropertiesAsync(cancellationToken: cancellationToken);

            // Extract code review metadata from blob metadata
            var codeReviewMetadata = ExtractCodeReviewMetadata(properties.Value.Metadata, blobName);

            // Check for duplicates if enabled
            if (_options.SkipDuplicates && codeReviewMetadata.PullRequestNumber.HasValue)
            {
                var existing = await _unitOfWork.CodeReviews.GetByPullRequestNumberAsync(
                    codeReviewMetadata.PullRequestNumber.Value,
                    cancellationToken);

                if (existing != null)
                {
                    _logger.LogInformation(
                        "Skipping duplicate code review. PR #{PrNumber} already migrated as Document ID {DocumentId}",
                        codeReviewMetadata.PullRequestNumber.Value,
                        existing.DocumentId);
                    return false;
                }
            }

            // Download blob content
            var downloadResult = await sourceBlobClient.DownloadContentAsync(cancellationToken);
            var blobContent = downloadResult.Value.Content.ToArray();

            // Create document in Document Service
            var document = new Document
            {
                TenantId = _options.DefaultTenantId,
                DocumentTypeId = _options.CodeReviewDocumentTypeId,
                Title = codeReviewMetadata.Title ?? $"Code Review - {blobName}",
                Description = codeReviewMetadata.Description,
                BlobPath = blobName, // Will be updated by DocumentService with new path
                MimeType = properties.Value.ContentType ?? "application/octet-stream",
                OriginalFileName = blobName,
                FileSizeBytes = properties.Value.ContentLength,
                Metadata = codeReviewMetadata.DocumentMetadata,
                Tags = codeReviewMetadata.Tags,
                CreatedBy = _options.SystemUser,
                CreatedAt = DateTime.UtcNow,
                IsCurrentVersion = true,
                Version = 1
            };

            // Use transaction to ensure atomicity
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                // Add document
                var createdDocument = await _unitOfWork.Documents.AddAsync(document, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Create CodeReview extension record
                var codeReview = new CodeReview
                {
                    DocumentId = createdDocument.Id,
                    PullRequestNumber = codeReviewMetadata.PullRequestNumber,
                    RepositoryName = codeReviewMetadata.RepositoryName,
                    BranchName = codeReviewMetadata.BranchName,
                    BaseBranchName = codeReviewMetadata.BaseBranchName,
                    Author = codeReviewMetadata.Author,
                    Reviewers = codeReviewMetadata.Reviewers,
                    Status = codeReviewMetadata.Status,
                    PullRequestUrl = codeReviewMetadata.PullRequestUrl,
                    CommitSha = codeReviewMetadata.CommitSha,
                    SubmittedAt = codeReviewMetadata.SubmittedAt,
                    ApprovedAt = codeReviewMetadata.ApprovedAt,
                    ApprovedBy = codeReviewMetadata.ApprovedBy,
                    MergedAt = codeReviewMetadata.MergedAt,
                    MergedBy = codeReviewMetadata.MergedBy,
                    FilesChanged = codeReviewMetadata.FilesChanged,
                    LinesAdded = codeReviewMetadata.LinesAdded,
                    LinesDeleted = codeReviewMetadata.LinesDeleted,
                    CommentCount = codeReviewMetadata.CommentCount,
                    SourceBlobPath = blobName,
                    MigratedAt = DateTime.UtcNow,
                    MigrationBatchId = batchId
                };

                await _unitOfWork.CodeReviews.AddAsync(codeReview, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation(
                    "Successfully migrated code review blob '{BlobName}' as Document ID {DocumentId}",
                    blobName,
                    createdDocument.Id);

                // Move blob to migrated container
                await MoveBlobAsync(
                    sourceBlobClient,
                    migratedContainer,
                    blobName,
                    new Dictionary<string, string>
                    {
                        ["DocumentId"] = createdDocument.Id.ToString(),
                        ["MigrationBatchId"] = batchId,
                        ["MigratedAt"] = DateTime.UtcNow.ToString("O")
                    },
                    cancellationToken);

                return true;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error migrating code review blob: {BlobName}", blobName);

            // Move blob to failed container
            try
            {
                var sourceBlobClient = sourceContainer.GetBlobClient(blobName);
                await MoveBlobAsync(
                    sourceBlobClient,
                    failedContainer,
                    blobName,
                    new Dictionary<string, string>
                    {
                        ["ErrorMessage"] = ex.Message,
                        ["ErrorTime"] = DateTime.UtcNow.ToString("O"),
                        ["MigrationBatchId"] = batchId
                    },
                    cancellationToken);
            }
            catch (Exception moveEx)
            {
                _logger.LogError(moveEx, "Error moving failed blob to failed container: {BlobName}", blobName);
            }

            return false;
        }
    }

    private CodeReviewMetadata ExtractCodeReviewMetadata(IDictionary<string, string> blobMetadata, string blobName)
    {
        var metadata = new CodeReviewMetadata
        {
            Title = blobMetadata.TryGetValue("Title", out var title) ? title : null,
            Description = blobMetadata.TryGetValue("Description", out var desc) ? desc : null
        };

        // Try to get pull request number
        if (blobMetadata.TryGetValue(_options.PullRequestNumberMetadataKey, out var prNumber) &&
            int.TryParse(prNumber, out var prNumberValue))
        {
            metadata.PullRequestNumber = prNumberValue;
        }

        // Try to extract structured code review metadata from JSON
        if (blobMetadata.TryGetValue(_options.MetadataKey, out var metadataJson))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(metadataJson);
                if (parsed != null)
                {
                    metadata.RepositoryName = GetStringValue(parsed, "RepositoryName");
                    metadata.BranchName = GetStringValue(parsed, "BranchName");
                    metadata.BaseBranchName = GetStringValue(parsed, "BaseBranchName");
                    metadata.Author = GetStringValue(parsed, "Author");
                    metadata.Status = GetStringValue(parsed, "Status");
                    metadata.PullRequestUrl = GetStringValue(parsed, "PullRequestUrl");
                    metadata.CommitSha = GetStringValue(parsed, "CommitSha");
                    metadata.ApprovedBy = GetStringValue(parsed, "ApprovedBy");
                    metadata.MergedBy = GetStringValue(parsed, "MergedBy");

                    metadata.FilesChanged = GetIntValue(parsed, "FilesChanged");
                    metadata.LinesAdded = GetIntValue(parsed, "LinesAdded");
                    metadata.LinesDeleted = GetIntValue(parsed, "LinesDeleted");
                    metadata.CommentCount = GetIntValue(parsed, "CommentCount");

                    metadata.SubmittedAt = GetDateTimeValue(parsed, "SubmittedAt");
                    metadata.ApprovedAt = GetDateTimeValue(parsed, "ApprovedAt");
                    metadata.MergedAt = GetDateTimeValue(parsed, "MergedAt");

                    // Handle reviewers array
                    if (parsed.TryGetValue("Reviewers", out var reviewersElement) &&
                        reviewersElement.ValueKind == JsonValueKind.Array)
                    {
                        metadata.Reviewers = reviewersElement.GetRawText();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing code review metadata JSON for blob: {BlobName}", blobName);
            }
        }

        // Extract additional metadata for Document.Metadata field
        metadata.DocumentMetadata = blobMetadata.TryGetValue("Metadata", out var docMetadata) ? docMetadata : null;
        metadata.Tags = blobMetadata.TryGetValue("Tags", out var tags) ? tags : null;

        return metadata;
    }

    private static string? GetStringValue(Dictionary<string, JsonElement> dict, string key)
    {
        return dict.TryGetValue(key, out var element) && element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;
    }

    private static int? GetIntValue(Dictionary<string, JsonElement> dict, string key)
    {
        return dict.TryGetValue(key, out var element) && element.TryGetInt32(out var value)
            ? value
            : null;
    }

    private static DateTime? GetDateTimeValue(Dictionary<string, JsonElement> dict, string key)
    {
        return dict.TryGetValue(key, out var element) && element.TryGetDateTime(out var value)
            ? value
            : null;
    }

    private static async Task MoveBlobAsync(
        BlobClient sourceBlobClient,
        BlobContainerClient destinationContainer,
        string blobName,
        Dictionary<string, string> additionalMetadata,
        CancellationToken cancellationToken)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var destinationBlobName = $"{timestamp}_{blobName}";
        var destinationBlobClient = destinationContainer.GetBlobClient(destinationBlobName);

        // Copy to destination
        await destinationBlobClient.StartCopyFromUriAsync(sourceBlobClient.Uri, cancellationToken: cancellationToken);

        // Wait for copy to complete
        var properties = await destinationBlobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
        while (properties.Value.CopyStatus == CopyStatus.Pending)
        {
            await Task.Delay(100, cancellationToken);
            properties = await destinationBlobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
        }

        // Set additional metadata
        if (additionalMetadata.Any())
        {
            var currentMetadata = properties.Value.Metadata;
            foreach (var kvp in additionalMetadata)
            {
                currentMetadata[kvp.Key] = kvp.Value;
            }
            await destinationBlobClient.SetMetadataAsync(currentMetadata, cancellationToken: cancellationToken);
        }

        // Delete source blob
        await sourceBlobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    private class CodeReviewMetadata
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int? PullRequestNumber { get; set; }
        public string? RepositoryName { get; set; }
        public string? BranchName { get; set; }
        public string? BaseBranchName { get; set; }
        public string? Author { get; set; }
        public string? Reviewers { get; set; }
        public string? Status { get; set; }
        public string? PullRequestUrl { get; set; }
        public string? CommitSha { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? MergedAt { get; set; }
        public string? MergedBy { get; set; }
        public int? FilesChanged { get; set; }
        public int? LinesAdded { get; set; }
        public int? LinesDeleted { get; set; }
        public int? CommentCount { get; set; }
        public string? DocumentMetadata { get; set; }
        public string? Tags { get; set; }
    }
}
