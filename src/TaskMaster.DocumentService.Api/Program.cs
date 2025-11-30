using Microsoft.EntityFrameworkCore;
using TaskMaster.DocumentService.Core.Interfaces;
using TaskMaster.DocumentService.Core.Services;
using TaskMaster.DocumentService.Data.Context;
using TaskMaster.DocumentService.Data.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Add OpenAPI/Swagger
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure database
var connectionString = builder.Configuration.GetConnectionString("DocumentServiceDb")
    ?? "Server=localhost;Database=TaskMaster.DocumentService;Integrated Security=true;TrustServerCertificate=true;";

builder.Services.AddDbContext<DocumentServiceDbContext>(options =>
    options.UseSqlServer(connectionString));

// Register repositories
builder.Services.AddScoped<ITemplateRepository, TemplateRepository>();

// Register services
builder.Services.AddScoped<ITemplateService, TemplateService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
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
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Document Service API v1");
    });
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();
