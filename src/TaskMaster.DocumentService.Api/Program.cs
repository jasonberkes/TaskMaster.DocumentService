using Microsoft.EntityFrameworkCore;
using TaskMaster.DocumentService.Api.Services;
using TaskMaster.DocumentService.Core.Services;
using TaskMaster.DocumentService.Data;
using TaskMaster.DocumentService.Data.Repositories;
using TaskMaster.DocumentService.Search.Configuration;
using TaskMaster.DocumentService.Search.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<DocumentServiceDbContext>(options =>
    options.UseSqlServer(connectionString));

// Configure Meilisearch
builder.Services.Configure<MeilisearchSettings>(
    builder.Configuration.GetSection("Meilisearch"));

// Register repositories
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();

// Register services
builder.Services.AddScoped<IMeilisearchService, MeilisearchService>();
builder.Services.AddScoped<IDocumentSearchService, DocumentSearchService>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<DocumentServiceDbContext>("database")
    .AddCheck<MeilisearchHealthCheck>("meilisearch");

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskMaster Document Service API v1");
        options.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

// Initialize Meilisearch index on startup
using (var scope = app.Services.CreateScope())
{
    var meilisearchService = scope.ServiceProvider.GetRequiredService<IMeilisearchService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        await meilisearchService.InitializeIndexAsync();
        logger.LogInformation("Meilisearch index initialized successfully");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Failed to initialize Meilisearch index on startup. Index will be initialized on first use.");
    }
}

app.Run();
