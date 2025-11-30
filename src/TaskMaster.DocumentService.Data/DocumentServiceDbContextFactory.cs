using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TaskMaster.DocumentService.Data;

/// <summary>
/// Design-time factory for DocumentServiceDbContext to enable EF Core migrations.
/// </summary>
public class DocumentServiceDbContextFactory : IDesignTimeDbContextFactory<DocumentServiceDbContext>
{
    /// <summary>
    /// Creates a new instance of DocumentServiceDbContext for design-time operations.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>A configured instance of DocumentServiceDbContext.</returns>
    public DocumentServiceDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DocumentServiceDbContext>();

        // Use a dummy connection string for migrations generation
        // The actual connection string will be provided at runtime
        optionsBuilder.UseSqlServer(
            "Server=(localdb)\\mssqllocaldb;Database=TaskMasterDocumentService;Trusted_Connection=True;MultipleActiveResultSets=true",
            sqlServerOptionsAction: sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            });

        return new DocumentServiceDbContext(optionsBuilder.Options);
    }
}
