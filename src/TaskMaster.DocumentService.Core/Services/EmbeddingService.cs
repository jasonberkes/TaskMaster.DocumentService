using Microsoft.Extensions.Logging;
using TaskMaster.AI.Core.Abstractions;

namespace TaskMaster.DocumentService.Core.Services;

/// <summary>
/// Service for generating document embeddings using Azure OpenAI
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Generate embedding for text content
    /// </summary>
    Task<EmbeddingResult> GenerateEmbeddingAsync(string text, long? documentId = null, CancellationToken ct = default);
    
    /// <summary>
    /// Generate embeddings for multiple texts
    /// </summary>
    Task<IReadOnlyList<EmbeddingResult>> GenerateEmbeddingsAsync(
        IEnumerable<(string Text, long? DocumentId)> items, 
        CancellationToken ct = default);
}

public record EmbeddingResult(
    bool Success,
    float[]? Embedding,
    int Dimensions,
    decimal CostUsd,
    string? ErrorMessage = null);

public class EmbeddingService : IEmbeddingService
{
    private readonly IAiGateway _aiGateway;
    private readonly ILogger<EmbeddingService> _logger;
    private const string Caller = "DocumentService";

    public EmbeddingService(IAiGateway aiGateway, ILogger<EmbeddingService> logger)
    {
        _aiGateway = aiGateway ?? throw new ArgumentNullException(nameof(aiGateway));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<EmbeddingResult> GenerateEmbeddingAsync(
        string text, 
        long? documentId = null, 
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new EmbeddingResult(false, null, 0, 0, "Text cannot be empty");
        }

        try
        {
            var request = new EmbeddingRequest
            {
                Text = text,
                Caller = Caller,
                Operation = "GenerateEmbedding",
                DocumentId = documentId
            };

            var response = await _aiGateway.GetEmbeddingAsync(request, ct);

            if (response.Success)
            {
                _logger.LogDebug(
                    "Generated embedding: Dims={Dimensions}, Cost=${Cost:F6}, DocId={DocumentId}",
                    response.Dimensions, response.CostUsd, documentId);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to generate embedding: {Error}, DocId={DocumentId}",
                    response.ErrorMessage, documentId);
            }

            return new EmbeddingResult(
                response.Success,
                response.Embedding,
                response.Dimensions,
                response.CostUsd,
                response.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding for document {DocumentId}", documentId);
            return new EmbeddingResult(false, null, 0, 0, ex.Message);
        }
    }

    public async Task<IReadOnlyList<EmbeddingResult>> GenerateEmbeddingsAsync(
        IEnumerable<(string Text, long? DocumentId)> items,
        CancellationToken ct = default)
    {
        var results = new List<EmbeddingResult>();
        
        foreach (var (text, documentId) in items)
        {
            if (ct.IsCancellationRequested)
                break;
                
            var result = await GenerateEmbeddingAsync(text, documentId, ct);
            results.Add(result);
        }

        var totalCost = results.Sum(r => r.CostUsd);
        var successCount = results.Count(r => r.Success);
        
        _logger.LogInformation(
            "Generated {SuccessCount}/{TotalCount} embeddings, TotalCost=${TotalCost:F4}",
            successCount, results.Count, totalCost);

        return results;
    }
}
