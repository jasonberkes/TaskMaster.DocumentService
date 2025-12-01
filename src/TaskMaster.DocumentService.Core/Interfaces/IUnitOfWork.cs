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
    /// Gets the document template repository.
    /// </summary>
    IDocumentTemplateRepository DocumentTemplates { get; }

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
}
