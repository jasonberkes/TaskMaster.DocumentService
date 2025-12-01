using TaskMaster.DocumentService.Core.Entities;

namespace TaskMaster.DocumentService.Core.Interfaces;

/// <summary>
/// Repository interface for CodeReview entity operations.
/// </summary>
public interface ICodeReviewRepository : IRepository<CodeReview, long>
{
    /// <summary>
    /// Gets a code review by pull request number.
    /// </summary>
    /// <param name="pullRequestNumber">The pull request number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The code review if found, otherwise null.</returns>
    Task<CodeReview?> GetByPullRequestNumberAsync(int pullRequestNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets code reviews by migration batch ID.
    /// </summary>
    /// <param name="migrationBatchId">The migration batch identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of code reviews from the specified batch.</returns>
    Task<IEnumerable<CodeReview>> GetByMigrationBatchIdAsync(string migrationBatchId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets code reviews by status.
    /// </summary>
    /// <param name="status">The code review status.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of code reviews with the specified status.</returns>
    Task<IEnumerable<CodeReview>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets code reviews by repository name.
    /// </summary>
    /// <param name="repositoryName">The repository name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of code reviews from the specified repository.</returns>
    Task<IEnumerable<CodeReview>> GetByRepositoryAsync(string repositoryName, CancellationToken cancellationToken = default);
}
