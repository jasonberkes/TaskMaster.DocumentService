using Microsoft.Extensions.Logging;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.Memory.Models;
using TaskMaster.Memory.Services;

namespace TaskMaster.DocumentService.Core.Services.Memory;

/// <summary>
/// Integrates DocumentService with TaskMaster.Memory for semantic search and graph relationships.
/// Stores document embeddings in Qdrant and creates graph nodes in FalkorDB.
/// </summary>
public class DocumentMemoryService : IDocumentMemoryService
{
    private readonly IVectorMemoryService _vectorMemory;
    private readonly IGraphMemoryService _graphMemory;
    private readonly ILogger<DocumentMemoryService> _logger;
    
    private const string CollectionName = "documents";
    private const string GraphName = "documents";
    private const string NodeLabel = "Document";

    public DocumentMemoryService(
        IVectorMemoryService vectorMemory,
        IGraphMemoryService graphMemory,
        ILogger<DocumentMemoryService> logger)
    {
        _vectorMemory = vectorMemory ?? throw new ArgumentNullException(nameof(vectorMemory));
        _graphMemory = graphMemory ?? throw new ArgumentNullException(nameof(graphMemory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<MemoryOperationResult> IndexDocumentAsync(
        Document document,
        string textContent,
        CancellationToken ct = default)
    {
        if (document == null)
            return new MemoryOperationResult(false, false, false, "Document cannot be null");

        if (string.IsNullOrWhiteSpace(textContent))
            return new MemoryOperationResult(false, false, false, "Text content cannot be empty");

        var documentId = document.Id.ToString();
        var embeddingStored = false;
        var graphNodeCreated = false;
        string? errorMessage = null;

        try
        {
            // 1. Store embedding in Qdrant
            var metadata = CreateEmbeddingMetadata(document);
            embeddingStored = await _vectorMemory.StoreEmbeddingAsync(
                documentId,
                textContent,
                metadata,
                CollectionName,
                ct);

            if (!embeddingStored)
            {
                _logger.LogWarning("Failed to store embedding for document {DocumentId}", document.Id);
            }

            // 2. Create graph node in FalkorDB
            var graphNode = CreateGraphNode(document);
            graphNodeCreated = await _graphMemory.CreateNodeAsync(graphNode, GraphName, ct);

            if (!graphNodeCreated)
            {
                _logger.LogWarning("Failed to create graph node for document {DocumentId}", document.Id);
            }

            var success = embeddingStored && graphNodeCreated;
            
            _logger.LogInformation(
                "Indexed document {DocumentId}: Embedding={EmbeddingStored}, Graph={GraphNodeCreated}",
                document.Id, embeddingStored, graphNodeCreated);

            return new MemoryOperationResult(success, embeddingStored, graphNodeCreated, errorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing document {DocumentId}", document.Id);
            return new MemoryOperationResult(false, embeddingStored, graphNodeCreated, ex.Message);
        }
    }

    public async Task<MemoryOperationResult> UpdateDocumentIndexAsync(
        Document document,
        string textContent,
        CancellationToken ct = default)
    {
        // For updates, we delete and re-create to ensure consistency
        var removeResult = await RemoveDocumentIndexAsync(document.Id, ct);
        
        if (!removeResult.Success)
        {
            _logger.LogWarning(
                "Failed to remove existing index for document {DocumentId} during update",
                document.Id);
        }

        return await IndexDocumentAsync(document, textContent, ct);
    }

    public async Task<MemoryOperationResult> RemoveDocumentIndexAsync(
        long documentId,
        CancellationToken ct = default)
    {
        var id = documentId.ToString();
        var embeddingDeleted = false;
        var graphNodeDeleted = false;
        string? errorMessage = null;

        try
        {
            // 1. Remove embedding from Qdrant
            embeddingDeleted = await _vectorMemory.DeleteEmbeddingAsync(id, CollectionName, ct);

            // 2. Remove graph node from FalkorDB (with relationships)
            graphNodeDeleted = await _graphMemory.DeleteNodeAsync(id, deleteRelationships: true, GraphName, ct);

            var success = embeddingDeleted || graphNodeDeleted;
            
            _logger.LogInformation(
                "Removed document index {DocumentId}: Embedding={EmbeddingDeleted}, Graph={GraphNodeDeleted}",
                documentId, embeddingDeleted, graphNodeDeleted);

            return new MemoryOperationResult(success, embeddingDeleted, graphNodeDeleted, errorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing document index {DocumentId}", documentId);
            return new MemoryOperationResult(false, embeddingDeleted, graphNodeDeleted, ex.Message);
        }
    }

    public async Task<IReadOnlyList<DocumentSearchResult>> SemanticSearchAsync(
        string query,
        int tenantId,
        int? documentTypeId = null,
        int maxResults = 10,
        float minScore = 0.7f,
        CancellationToken ct = default)
    {
        try
        {
            // Build filter for tenant isolation
            var filter = new Dictionary<string, object>
            {
                ["tenantId"] = tenantId
            };

            if (documentTypeId.HasValue)
            {
                filter["documentTypeId"] = documentTypeId.Value;
            }

            var results = await _vectorMemory.SimilaritySearchWithFilterAsync(
                query,
                filter,
                maxResults,
                minScore,
                CollectionName,
                ct);

            return results
                .Select(r => new DocumentSearchResult(
                    long.Parse(r.Id),
                    GetTitleFromMetadata(r.Metadata),
                    r.Score,
                    r.Text.Length > 200 ? r.Text[..200] + "..." : r.Text))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing semantic search for query: {Query}", query);
            return Array.Empty<DocumentSearchResult>();
        }
    }

    public async Task<IReadOnlyList<RelatedDocument>> GetRelatedDocumentsAsync(
        long documentId,
        int maxDepth = 2,
        int maxResults = 10,
        CancellationToken ct = default)
    {
        try
        {
            var id = documentId.ToString();
            var relatedDocs = new List<RelatedDocument>();
            
            // Get outgoing relationships
            var outgoing = await _graphMemory.GetOutgoingRelationshipsAsync(
                id,
                relationshipType: null,
                
                graphName: GraphName,
                cancellationToken: ct);

            foreach (var rel in outgoing)
            {
                var targetNode = await _graphMemory.GetNodeAsync(rel.TargetNodeId, GraphName, ct);
                if (targetNode != null)
                {
                    var title = targetNode.Properties.TryGetValue("title", out var t) 
                        ? t?.ToString() ?? "Untitled" 
                        : "Untitled";
                    
                    relatedDocs.Add(new RelatedDocument(
                        long.Parse(rel.TargetNodeId),
                        title,
                        rel.Type,
                        1));
                }
            }

            // Get incoming relationships
            var incoming = await _graphMemory.GetIncomingRelationshipsAsync(
                id,
                relationshipType: null,
                
                graphName: GraphName,
                cancellationToken: ct);

            foreach (var rel in incoming)
            {
                var sourceNode = await _graphMemory.GetNodeAsync(rel.SourceNodeId, GraphName, ct);
                if (sourceNode != null)
                {
                    var title = sourceNode.Properties.TryGetValue("title", out var t) 
                        ? t?.ToString() ?? "Untitled" 
                        : "Untitled";
                    
                    relatedDocs.Add(new RelatedDocument(
                        long.Parse(rel.SourceNodeId),
                        title,
                        rel.Type,
                        1));
                }
            }

            return relatedDocs.Take(maxResults).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting related documents for {DocumentId}", documentId);
            return Array.Empty<RelatedDocument>();
        }
    }

    public async Task<bool> CreateDocumentRelationshipAsync(
        long sourceDocumentId,
        long targetDocumentId,
        string relationshipType,
        Dictionary<string, object>? properties = null,
        CancellationToken ct = default)
    {
        try
        {
            var props = properties ?? new Dictionary<string, object>();
            if (!props.ContainsKey("createdAt"))
            {
                props["createdAt"] = DateTime.UtcNow.ToString("O");
            }

            var created = await _graphMemory.StoreRelationshipAsync(
                sourceDocumentId.ToString(),
                relationshipType,
                targetDocumentId.ToString(),
                props,
                GraphName,
                ct);
            
            _logger.LogInformation(
                "Created relationship {Type} from {Source} to {Target}: {Success}",
                relationshipType, sourceDocumentId, targetDocumentId, created);

            return created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error creating relationship {Type} from {Source} to {Target}",
                relationshipType, sourceDocumentId, targetDocumentId);
            return false;
        }
    }

    public async Task<BatchMemoryOperationResult> IndexDocumentsBatchAsync(
        IEnumerable<(Document Document, string TextContent)> documents,
        CancellationToken ct = default)
    {
        var docList = documents.ToList();
        var errors = new List<string>();

        // Batch store embeddings
        var embeddingItems = docList
            .Where(d => !string.IsNullOrWhiteSpace(d.TextContent))
            .Select(d => (
                Id: d.Document.Id.ToString(),
                Text: d.TextContent,
                Metadata: CreateEmbeddingMetadata(d.Document)
            ))
            .ToList();

        var embeddingsStored = await _vectorMemory.StoreManyEmbeddingsAsync(
            embeddingItems.Select(e => (e.Id, e.Text, e.Metadata)),
            CollectionName,
            ct);

        _logger.LogInformation("Batch stored {Count} embeddings", embeddingsStored);

        // Create graph nodes
        var graphNodes = docList.Select(d => CreateGraphNode(d.Document)).ToList();
        var nodesCreated = await _graphMemory.CreateManyNodesAsync(graphNodes, GraphName, ct);

        _logger.LogInformation("Batch created {Count} graph nodes", nodesCreated);

        var successCount = Math.Min(embeddingsStored, nodesCreated);

        return new BatchMemoryOperationResult(
            docList.Count,
            successCount,
            docList.Count - successCount,
            errors);
    }

    #region Private Helpers

    private static string GetTitleFromMetadata(EmbeddingMetadata? metadata)
    {
        if (metadata?.CustomProperties.TryGetValue("title", out var title) == true)
        {
            return title?.ToString() ?? "Untitled";
        }
        return "Untitled";
    }

    private static EmbeddingMetadata CreateEmbeddingMetadata(Document document)
    {
        return new EmbeddingMetadata
        {
            Source = "DocumentService",
            Category = $"DocumentType_{document.DocumentTypeId}",
            CreatedAt = document.CreatedAt,
            ReferenceId = document.Id.ToString(),
            Tags = ParseTags(document.Tags),
            CustomProperties = new Dictionary<string, object>
            {
                ["documentId"] = document.Id,
                ["tenantId"] = document.TenantId,
                ["documentTypeId"] = document.DocumentTypeId,
                ["title"] = document.Title,
                ["isDeleted"] = document.DeletedAt.HasValue
            }
        };
    }

    private static List<string> ParseTags(string? tagsJson)
    {
        if (string.IsNullOrWhiteSpace(tagsJson))
            return new List<string>();

        // Tags might be comma-separated or JSON array
        if (tagsJson.StartsWith("["))
        {
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<string>>(tagsJson) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        return tagsJson.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .ToList();
    }

    private static GraphNode CreateGraphNode(Document document)
    {
        return new GraphNode
        {
            Id = document.Id.ToString(),
            Labels = new List<string> { NodeLabel, $"Type_{document.DocumentTypeId}" },
            Properties = new Dictionary<string, object>
            {
                ["documentId"] = document.Id,
                ["tenantId"] = document.TenantId,
                ["title"] = document.Title,
                ["documentTypeId"] = document.DocumentTypeId,
                ["createdAt"] = document.CreatedAt.ToString("O"),
                ["isDeleted"] = document.DeletedAt.HasValue
            }
        };
    }

    #endregion
}
