using Microsoft.EntityFrameworkCore;
using TaskMaster.DocumentService.Core.Entities;
using TaskMaster.DocumentService.Core.Interfaces;

namespace TaskMaster.DocumentService.Data.Repositories;

/// <summary>
/// Repository implementation for DocumentType entity operations.
/// </summary>
public class DocumentTypeRepository : Repository<DocumentType, int>, IDocumentTypeRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentTypeRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public DocumentTypeRepository(DocumentServiceDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<DocumentType?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(dt => dt.Name == name, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DocumentType>> GetActiveDocumentTypesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(dt => dt.IsActive)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DocumentType>> GetIndexableTypesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(dt => dt.IsContentIndexed && dt.IsActive)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DocumentType>> GetTypesWithExtensionTablesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(dt => dt.HasExtensionTable && dt.IsActive)
            .ToListAsync(cancellationToken);
    }
}
