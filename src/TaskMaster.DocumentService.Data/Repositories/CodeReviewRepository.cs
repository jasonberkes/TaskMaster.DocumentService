using Microsoft.EntityFrameworkCore;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Interfaces;

namespace TaskMaster.DocumentService.Data.Repositories;

/// <summary>
/// Repository implementation for CodeReview entity operations.
/// </summary>
public class CodeReviewRepository : Repository<CodeReview, long>, ICodeReviewRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CodeReviewRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public CodeReviewRepository(DocumentServiceDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<CodeReview?> GetByPullRequestNumberAsync(int pullRequestNumber, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(cr => cr.Document)
            .FirstOrDefaultAsync(cr => cr.PullRequestNumber == pullRequestNumber, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<CodeReview>> GetByMigrationBatchIdAsync(string migrationBatchId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(cr => cr.Document)
            .Where(cr => cr.MigrationBatchId == migrationBatchId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<CodeReview>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(cr => cr.Document)
            .Where(cr => cr.Status == status)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<CodeReview>> GetByRepositoryAsync(string repositoryName, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(cr => cr.Document)
            .Where(cr => cr.RepositoryName == repositoryName)
            .ToListAsync(cancellationToken);
    }
}
