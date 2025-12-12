using TaskMaster.DocumentService.SDK.DTOs;

namespace TaskMaster.DocumentService.SDK.Interfaces;

/// <summary>
/// Client interface for document search operations.
/// </summary>
public interface ISearchClient
{
    /// <summary>
    /// Searches for documents using the specified criteria.
    /// </summary>
    /// <param name="request">The search request parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Search results with pagination information.</returns>
    Task<SearchResultDto> SearchAsync(SearchRequestDto request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the health status of the search service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the search service is healthy.</returns>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}
