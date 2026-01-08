using TaskMaster.DocumentService.Core.Entities;

namespace TaskMaster.DocumentService.Core.Services.Memory;

/// <summary>
/// Service for managing document embeddings in Qdrant and graph nodes in FalkorDB.
/// Integrates DocumentService with TaskMaster.Memory for semantic search capabilities.
/// </summary>
public interface IDocumentMemoryService
{
    /// <summary>
    /// Stores a document's embedding in Qdrant and creates a graph node in FalkorDB.
    /// Called after document creation.
    /// </summary>
    Task<MemoryOperationResult> IndexDocumentAsync(
        Document document,
        string textContent,
        CancellationToken ct = default);

    /// <summary>
    /// Updates a document's embedding and graph node.
    /// Called after document update.
    /// </summary>
    Task<MemoryOperationResult> UpdateDocumentIndexAsync(
        Document document,
        string textContent,
        CancellationToken ct = default);

    /// <summary>
    /// Removes a document's embedding from Qdrant and graph node from FalkorDB.
    /// Called after document deletion.
    /// </summary>
    Task<MemoryOperationResult> RemoveDocumentIndexAsync(
        long documentId,
        CancellationToken ct = default);

    /// <summary>
    /// Performs semantic search across documents using vector similarity.
    /// </summary>
    Task<IReadOnlyList<DocumentSearchResult>> SemanticSearchAsync(
        string query,
        int tenantId,
        int? documentTypeId = null,
        int maxResults = 10,
        float minScore = 0.7f,
        CancellationToken ct = default);

    /// <summary>
    /// Gets related documents via graph traversal.
    /// </summary>
    Task<IReadOnlyList<RelatedDocument>> GetRelatedDocumentsAsync(
        long documentId,
        int maxDepth = 2,
        int maxResults = 10,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a relationship between two documents in the graph.
    /// </summary>
    Task<bool> CreateDocumentRelationshipAsync(
        long sourceDocumentId,
        long targetDocumentId,
        string relationshipType,
        Dictionary<string, object>? properties = null,
        CancellationToken ct = default);

    /// <summary>
    /// Batch indexes multiple documents.
    /// </summary>
    Task<BatchMemoryOperationResult> IndexDocumentsBatchAsync(
        IEnumerable<(Document Document, string TextContent)> documents,
        CancellationToken ct = default);
}

public record MemoryOperationResult(
    bool Success,
    bool EmbeddingStored,
    bool GraphNodeCreated,
    string? ErrorMessage = null);

public record BatchMemoryOperationResult(
    int TotalCount,
    int SuccessCount,
    int FailedCount,
    IReadOnlyList<string> Errors);

public record DocumentSearchResult(
    long DocumentId,
    string Title,
    float Score,
    string? Snippet);

public record RelatedDocument(
    long DocumentId,
    string Title,
    string RelationshipType,
    int Depth);
