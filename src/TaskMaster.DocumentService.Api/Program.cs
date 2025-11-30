using Azure.Storage.Blobs;
using TaskMaster.DocumentService.Core.Interfaces;
using TaskMaster.DocumentService.Core.Services;
using TaskMaster.DocumentService.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add controllers
builder.Services.AddControllers();

// Add Document Service Data layer (DbContext, Repositories, UnitOfWork)
builder.Services.AddDocumentServiceData(builder.Configuration);

// Configure Blob Storage
builder.Services.Configure<BlobStorageOptions>(
    builder.Configuration.GetSection(BlobStorageOptions.SectionName));

builder.Services.AddSingleton(serviceProvider =>
{
    var connectionString = builder.Configuration.GetConnectionString("BlobStorage")
        ?? builder.Configuration["BlobStorage:ConnectionString"];

    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("BlobStorage connection string is not configured.");
    }

    return new BlobServiceClient(connectionString);
});

builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();

// Register migration service
// WI #2298: Document Service: Migrate Existing Code Reviews from TaskMaster.Platform
builder.Services.AddScoped<ICodeReviewMigrationService, CodeReviewMigrationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Map controllers
app.MapControllers();

app.Run();
