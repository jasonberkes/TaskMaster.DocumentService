using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using TaskMaster.DocumentService.Core.Interfaces;
using TaskMaster.DocumentService.Data;
using TaskMaster.DocumentService.Data.Repositories;
using TaskMaster.DocumentService.Processing.BackgroundServices;
using TaskMaster.DocumentService.Processing.Configuration;
using TaskMaster.DocumentService.Processing.Interfaces;
using TaskMaster.DocumentService.Processing.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddOpenApi();

// Configure options
builder.Services.Configure<BlobStorageOptions>(
    builder.Configuration.GetSection(BlobStorageOptions.SectionName));
builder.Services.Configure<InboxProcessorOptions>(
    builder.Configuration.GetSection(InboxProcessorOptions.SectionName));

// Configure database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<DocumentDbContext>(options =>
    options.UseSqlServer(connectionString));

// Configure Azure Blob Storage
var blobConnectionString = builder.Configuration.GetSection(BlobStorageOptions.SectionName)
    .GetValue<string>("ConnectionString");
builder.Services.AddSingleton(x => new BlobServiceClient(blobConnectionString));

// Register repositories
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();

// Register processing services
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
builder.Services.AddScoped<IDocumentProcessor, DocumentProcessor>();

// Register text extractors
builder.Services.AddScoped<ITextExtractor, PdfTextExtractor>();
builder.Services.AddScoped<ITextExtractor, OpenXmlTextExtractor>();
builder.Services.AddScoped<ITextExtractor, PlainTextExtractor>();

// Register background service
builder.Services.AddHostedService<InboxProcessorBackgroundService>();

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck")
    .WithTags("Health");

app.Run();
