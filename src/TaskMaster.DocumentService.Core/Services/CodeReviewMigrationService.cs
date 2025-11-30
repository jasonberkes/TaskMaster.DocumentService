using System.Text.Json;
using Microsoft.Extensions.Logging;
using TaskMaster.DocumentService.Core.DTOs;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Interfaces;

namespace TaskMaster.DocumentService.Core.Services;

/// <summary>
/// Service for migrating code reviews from TaskMaster.Platform to Document Service.
/// WI #2298: Document Service: Migrate Existing Code Reviews from TaskMaster.Platform
/// </summary>
public class CodeReviewMigrationService : ICodeReviewMigrationService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentTypeRepository _documentTypeRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CodeReviewMigrationService> _logger;
    private const string CodeReviewDocumentTypeName = "CodeReview";

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeReviewMigrationService"/> class.
    /// </summary>
    /// <param name="documentRepository">The document repository.</param>
    /// <param name="documentTypeRepository">The document type repository.</param>
    /// <param name="tenantRepository">The tenant repository.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    /// <param name="logger">The logger.</param>
    public CodeReviewMigrationService(
        IDocumentRepository documentRepository,
        IDocumentTypeRepository documentTypeRepository,
        ITenantRepository tenantRepository,
        IUnitOfWork unitOfWork,
        ILogger<CodeReviewMigrationService> logger)
    {
        _documentRepository = documentRepository ?? throw new ArgumentNullException(nameof(documentRepository));
        _documentTypeRepository = documentTypeRepository ?? throw new ArgumentNullException(nameof(documentTypeRepository));
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<MigrateCodeReviewsResponse> MigrateCodeReviewsAsync(
        MigrateCodeReviewsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (request.CodeReviews == null || !request.CodeReviews.Any())
            throw new ArgumentException("No code reviews provided for migration.", nameof(request));

        var response = new MigrateCodeReviewsResponse
        {
            StartedAt = DateTime.UtcNow
        };

        _logger.LogInformation(
            "Starting code review migration. TenantId: {TenantId}, Count: {Count}, BatchSize: {BatchSize}",
            request.TenantId,
            request.CodeReviews.Count,
            request.BatchSize);

        try
        {
            // Validate tenant
            var tenantExists = await ValidateTenantAsync(request.TenantId, cancellationToken);
            if (!tenantExists)
            {
                response.Errors.Add($"Tenant with ID {request.TenantId} does not exist or is not active.");
                response.CompletedAt = DateTime.UtcNow;
                return response;
            }

            // Ensure CodeReview document type exists
            var documentTypeId = await EnsureCodeReviewDocumentTypeAsync(cancellationToken);

            // Process code reviews in batches
            var batches = request.CodeReviews
                .Select((review, index) => new { review, index })
                .GroupBy(x => x.index / request.BatchSize)
                .Select(g => g.Select(x => x.review).ToList())
                .ToList();

            _logger.LogInformation("Processing {BatchCount} batches", batches.Count);

            foreach (var batch in batches)
            {
                await ProcessBatchAsync(
                    batch,
                    request.TenantId,
                    documentTypeId,
                    request.SkipExisting,
                    response,
                    cancellationToken);
            }

            response.CompletedAt = DateTime.UtcNow;

            _logger.LogInformation(
                "Code review migration completed. Migrated: {Migrated}, Skipped: {Skipped}, Failed: {Failed}, Duration: {Duration}",
                response.MigratedCount,
                response.SkippedCount,
                response.FailedCount,
                response.Duration);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error during code review migration");
            response.Errors.Add($"Fatal error: {ex.Message}");
            response.CompletedAt = DateTime.UtcNow;
            return response;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ValidateTenantAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
            return tenant != null && tenant.IsActive;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating tenant {TenantId}", tenantId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<int> EnsureCodeReviewDocumentTypeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if CodeReview document type already exists
            var existingTypes = await _documentTypeRepository.GetAllAsync(cancellationToken);
            var codeReviewType = existingTypes.FirstOrDefault(dt =>
                dt.Name.Equals(CodeReviewDocumentTypeName, StringComparison.OrdinalIgnoreCase));

            if (codeReviewType != null)
            {
                _logger.LogInformation("CodeReview document type already exists with ID {Id}", codeReviewType.Id);
                return codeReviewType.Id;
            }

            // Create new CodeReview document type
            var newDocumentType = new DocumentType
            {
                Name = CodeReviewDocumentTypeName,
                DisplayName = "Code Review",
                Description = "AI-powered code review results migrated from TaskMaster.Platform",
                MetadataSchema = GetCodeReviewMetadataSchema(),
                DefaultTags = "[\"code-review\",\"ai-generated\",\"quality-assurance\"]",
                Icon = "code-review",
                IsContentIndexed = true,
                HasExtensionTable = false,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            await _documentTypeRepository.AddAsync(newDocumentType, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created CodeReview document type with ID {Id}", newDocumentType.Id);
            return newDocumentType.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring CodeReview document type exists");
            throw;
        }
    }

    /// <summary>
    /// Processes a batch of code reviews.
    /// </summary>
    private async Task ProcessBatchAsync(
        List<CodeReviewMigrationDto> batch,
        int tenantId,
        int documentTypeId,
        bool skipExisting,
        MigrateCodeReviewsResponse response,
        CancellationToken cancellationToken)
    {
        foreach (var review in batch)
        {
            try
            {
                // Check if already migrated (based on blob path or metadata)
                if (skipExisting && !string.IsNullOrEmpty(review.BlobPath))
                {
                    var existingDocs = await _documentRepository.GetByTenantIdAsync(tenantId, false, cancellationToken);
                    var alreadyMigrated = existingDocs.Any(d =>
                        d.BlobPath == review.BlobPath ||
                        (d.Metadata != null && d.Metadata.Contains($"\"sourceCodeReviewId\":{review.SourceCodeReviewId}")));

                    if (alreadyMigrated)
                    {
                        _logger.LogDebug("Skipping already migrated code review {SourceId}", review.SourceCodeReviewId);
                        response.SkippedCount++;
                        continue;
                    }
                }

                // Transform to Document entity
                var document = TransformCodeReviewToDocument(review, tenantId, documentTypeId);

                // Add to repository
                await _documentRepository.AddAsync(document, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                response.MigratedCount++;

                _logger.LogDebug(
                    "Migrated code review {SourceId} to document {DocumentId}",
                    review.SourceCodeReviewId,
                    document.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error migrating code review {SourceId}",
                    review.SourceCodeReviewId);

                response.FailedCount++;
                response.Errors.Add($"Code review {review.SourceCodeReviewId}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Transforms a code review DTO to a Document entity.
    /// </summary>
    private Document TransformCodeReviewToDocument(
        CodeReviewMigrationDto review,
        int tenantId,
        int documentTypeId)
    {
        // Generate title from PR and repository info
        var repoInfo = !string.IsNullOrEmpty(review.GitHubRepoOwner) && !string.IsNullOrEmpty(review.GitHubRepoName)
            ? $"{review.GitHubRepoOwner}/{review.GitHubRepoName}"
            : "Unknown Repository";

        var title = $"Code Review: PR #{review.PrNumber} - {repoInfo}";

        // Build extended metadata JSON
        var metadata = new
        {
            sourceCodeReviewId = review.SourceCodeReviewId,
            prNumber = review.PrNumber,
            gitHubRepoOwner = review.GitHubRepoOwner,
            gitHubRepoName = review.GitHubRepoName,
            workItemId = review.WorkItemId,
            reviewedAt = review.ReviewedAt,
            qualityScore = review.QualityScore,
            testStatus = review.TestStatus,
            buildStatus = review.BuildStatus,
            breakingChanges = review.BreakingChanges,
            recommendation = review.Recommendation,
            inputTokens = review.InputTokens,
            outputTokens = review.OutputTokens,
            apiCost = review.ApiCost
        };

        // Build tags from review characteristics
        var tags = new List<string> { "code-review", "migrated" };
        if (review.QualityScore >= 90) tags.Add("high-quality");
        else if (review.QualityScore >= 70) tags.Add("medium-quality");
        else tags.Add("needs-improvement");

        if (review.BreakingChanges) tags.Add("breaking-changes");
        if (review.SecurityIssues != null && review.SecurityIssues != "[]") tags.Add("security-issues");
        if (review.PerformanceIssues != null && review.PerformanceIssues != "[]") tags.Add("performance-issues");

        // Determine blob path - use existing or generate placeholder
        var blobPath = review.BlobPath ??
                      $"code-reviews/migrated/{review.ReviewedAt:yyyy}/{review.ReviewedAt:MM}/{review.PrNumber}/{review.SourceCodeReviewId}.json";

        // Build description from review findings
        var description = $"Quality Score: {review.QualityScore}/100 | " +
                         $"Recommendation: {review.Recommendation} | " +
                         $"Reviewed: {review.ReviewedAt:yyyy-MM-dd HH:mm:ss} UTC";

        return new Document
        {
            TenantId = tenantId,
            DocumentTypeId = documentTypeId,
            Title = title,
            Description = description,
            BlobPath = blobPath,
            ContentHash = null, // No content hash available from source
            FileSizeBytes = null,
            MimeType = "application/json",
            OriginalFileName = $"pr-{review.PrNumber}-review.json",
            Metadata = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }),
            Tags = JsonSerializer.Serialize(tags),
            MeilisearchId = null,
            LastIndexedAt = null,
            ExtractedText = BuildExtractedText(review),
            Version = 1,
            ParentDocumentId = null,
            IsCurrentVersion = true,
            CreatedAt = review.CreatedAt,
            CreatedBy = "CodeReviewMigration",
            UpdatedAt = null,
            UpdatedBy = null,
            IsDeleted = false,
            DeletedAt = null,
            DeletedBy = null,
            DeletedReason = null,
            IsArchived = false,
            ArchivedAt = null
        };
    }

    /// <summary>
    /// Builds extracted text from code review findings for search indexing.
    /// </summary>
    private string BuildExtractedText(CodeReviewMigrationDto review)
    {
        var textParts = new List<string>
        {
            $"Pull Request #{review.PrNumber}",
            $"Work Item #{review.WorkItemId}",
            $"Quality Score: {review.QualityScore}",
            $"Recommendation: {review.Recommendation}",
            $"Test Status: {review.TestStatus}",
            $"Build Status: {review.BuildStatus}"
        };

        if (!string.IsNullOrEmpty(review.GitHubRepoOwner))
            textParts.Add($"Repository: {review.GitHubRepoOwner}/{review.GitHubRepoName}");

        if (!string.IsNullOrEmpty(review.PositiveFindings) && review.PositiveFindings != "[]")
            textParts.Add($"Positive Findings: {review.PositiveFindings}");

        if (!string.IsNullOrEmpty(review.PotentialConcerns) && review.PotentialConcerns != "[]")
            textParts.Add($"Concerns: {review.PotentialConcerns}");

        if (!string.IsNullOrEmpty(review.Suggestions) && review.Suggestions != "[]")
            textParts.Add($"Suggestions: {review.Suggestions}");

        if (!string.IsNullOrEmpty(review.SecurityIssues) && review.SecurityIssues != "[]")
            textParts.Add($"Security Issues: {review.SecurityIssues}");

        if (!string.IsNullOrEmpty(review.PerformanceIssues) && review.PerformanceIssues != "[]")
            textParts.Add($"Performance Issues: {review.PerformanceIssues}");

        return string.Join(" | ", textParts);
    }

    /// <summary>
    /// Gets the JSON schema for code review metadata.
    /// </summary>
    private string GetCodeReviewMetadataSchema()
    {
        var schema = new
        {
            type = "object",
            properties = new
            {
                sourceCodeReviewId = new { type = "integer", description = "Original code review ID from TaskMaster.Platform" },
                prNumber = new { type = "integer", description = "Pull request number" },
                gitHubRepoOwner = new { type = "string", description = "GitHub repository owner" },
                gitHubRepoName = new { type = "string", description = "GitHub repository name" },
                workItemId = new { type = "integer", description = "Associated work item ID" },
                reviewedAt = new { type = "string", format = "date-time", description = "When the review was performed" },
                qualityScore = new { type = "integer", minimum = 0, maximum = 100, description = "Overall quality score" },
                testStatus = new { type = "string", description = "Test execution status" },
                buildStatus = new { type = "string", description = "Build status" },
                breakingChanges = new { type = "boolean", description = "Whether breaking changes were detected" },
                recommendation = new { type = "string", description = "Review recommendation" },
                inputTokens = new { type = "integer", description = "Input tokens used" },
                outputTokens = new { type = "integer", description = "Output tokens generated" },
                apiCost = new { type = "number", description = "API cost in USD" }
            },
            required = new[] { "sourceCodeReviewId", "prNumber", "workItemId", "qualityScore" }
        };

        return JsonSerializer.Serialize(schema, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }
}
