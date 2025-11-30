using Microsoft.EntityFrameworkCore;
using TaskMaster.DocumentService.Data;

namespace TaskMaster.DocumentService.Core.Tests.Helpers;

/// <summary>
/// Factory for creating in-memory database contexts for testing.
/// </summary>
public static class TestDbContextFactory
{
    /// <summary>
    /// Creates a new in-memory database context with a unique database name.
    /// </summary>
    /// <returns>A new DocumentServiceDbContext configured for in-memory testing.</returns>
    public static DocumentServiceDbContext Create()
    {
        var options = new DbContextOptionsBuilder<DocumentServiceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new DocumentServiceDbContext(options);
    }

    /// <summary>
    /// Creates a new in-memory database context with a specific database name.
    /// </summary>
    /// <param name="databaseName">The database name.</param>
    /// <returns>A new DocumentServiceDbContext configured for in-memory testing.</returns>
    public static DocumentServiceDbContext Create(string databaseName)
    {
        var options = new DbContextOptionsBuilder<DocumentServiceDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;

        return new DocumentServiceDbContext(options);
    }
}
