using Microsoft.EntityFrameworkCore;
using TaskMaster.DocumentService.Core.Interfaces;
using TaskMaster.DocumentService.Core.Services;
using TaskMaster.DocumentService.Data.Data;
using TaskMaster.DocumentService.Data.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Add OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure DbContext
builder.Services.AddDbContext<DocumentServiceDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DocumentServiceDb")
        ?? throw new InvalidOperationException("Connection string 'DocumentServiceDb' not found.");
    options.UseSqlServer(connectionString);
});

// Register repositories
builder.Services.AddScoped<ICollectionRepository, CollectionRepository>();

// Register services
builder.Services.AddScoped<ICollectionService, CollectionService>();

// Add CORS if needed
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
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

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();
