using Microsoft.EntityFrameworkCore.Storage;
using TaskMaster.DocumentService.Core.Interfaces;
using TaskMaster.DocumentService.Data.Repositories;

namespace TaskMaster.DocumentService.Data;

/// <summary>
/// Unit of Work implementation for coordinating database transactions across repositories.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly DocumentServiceDbContext _context;
    private IDbContextTransaction? _transaction;
    private bool _disposed;

    private ITenantRepository? _tenants;
    private IDocumentTypeRepository? _documentTypes;
    private IDocumentRepository? _documents;
    private ICollectionRepository? _collections;
    private IDocumentTemplateRepository? _documentTemplates;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWork"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public UnitOfWork(DocumentServiceDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc/>
    public ITenantRepository Tenants
    {
        get
        {
            _tenants ??= new TenantRepository(_context);
            return _tenants;
        }
    }

    /// <inheritdoc/>
    public IDocumentTypeRepository DocumentTypes
    {
        get
        {
            _documentTypes ??= new DocumentTypeRepository(_context);
            return _documentTypes;
        }
    }

    /// <inheritdoc/>
    public IDocumentRepository Documents
    {
        get
        {
            _documents ??= new DocumentRepository(_context);
            return _documents;
        }
    }

    /// <inheritdoc/>
    public ICollectionRepository Collections
    {
        get
        {
            _collections ??= new CollectionRepository(_context);
            return _collections;
        }
    }

    /// <inheritdoc/>
    public IDocumentTemplateRepository DocumentTemplates
    {
        get
        {
            _documentTemplates ??= new DocumentTemplateRepository(_context);
            return _documentTemplates;
        }
    }

    /// <inheritdoc/>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No active transaction to commit.");
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await _transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            _transaction.Dispose();
            _transaction = null;
        }
    }

    /// <inheritdoc/>
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            _transaction.Dispose();
            _transaction = null;
        }
    }

    /// <summary>
    /// Disposes the unit of work and associated resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the unit of work and associated resources.
    /// </summary>
    /// <param name="disposing">Whether to dispose managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _transaction?.Dispose();
                _context.Dispose();
            }

            _disposed = true;
        }
    }
}
