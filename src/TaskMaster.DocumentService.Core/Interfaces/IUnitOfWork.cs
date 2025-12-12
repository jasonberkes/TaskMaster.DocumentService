namespace TaskMaster.DocumentService.Core.Interfaces;

/// <summary>
/// Unit of Work interface for coordinating database transactions across repositories.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Gets the tenant repository.
    /// </summary>
    ITenantRepository Tenants { get; }

    /// <summary>
    /// Gets the document type repository.
    /// </summary>
    IDocumentTypeRepository DocumentTypes { get; }

    /// <summary>
    /// Gets the document repository.
    /// </summary>
    IDocumentRepository Documents { get; }

    /// <summary>
    /// Gets the collection repository.
    /// </summary>
    ICollectionRepository Collections { get; }

    /// <summary>
    /// Gets the template repository.
    /// </summary>
    ITemplateRepository Templates { get; }

    /// <summary>
    /// Gets the code review repository.
    /// </summary>
    ICodeReviewRepository CodeReviews { get; }

    /// <summary>
    /// Saves all changes made in this context to the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a new database transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current database transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the current database transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an operation within a transaction using the database execution strategy.
    /// This method should be used when the execution strategy (e.g., SqlServerRetryingExecutionStrategy) is enabled.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the operation.</typeparam>
    /// <param name="operation">The operation to execute within a transaction.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default);
}
