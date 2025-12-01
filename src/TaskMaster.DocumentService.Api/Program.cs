using System.Text;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using TaskMaster.DocumentService.Api.Authentication;
using TaskMaster.DocumentService.Api.Authorization;
using TaskMaster.DocumentService.Core.Interfaces;
using TaskMaster.DocumentService.Core.Services;
using TaskMaster.DocumentService.Data;
using TaskMaster.DocumentService.Processing.Extensions;
using TaskMaster.DocumentService.Search.Extensions;
using TaskMaster.DocumentService.Search.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Configure Authentication Options
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<ApiKeyOptions>(
    builder.Configuration.GetSection(ApiKeyOptions.SectionName));

// Configure Authentication
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>();
var apiKeyOptions = builder.Configuration.GetSection(ApiKeyOptions.SectionName).Get<ApiKeyOptions>();

builder.Services.AddAuthentication(options =>
{
    // Default to JWT Bearer authentication
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    if (jwtOptions != null && !string.IsNullOrEmpty(jwtOptions.SecretKey))
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = jwtOptions.ValidateLifetime,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.ValidIssuer,
            ValidAudience = jwtOptions.ValidAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
            ClockSkew = TimeSpan.FromMinutes(jwtOptions.ClockSkewMinutes)
        };
        options.RequireHttpsMetadata = jwtOptions.RequireHttpsMetadata;

        // Extract TenantId from JWT claims if present
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var claimsIdentity = context.Principal?.Identity as System.Security.Claims.ClaimsIdentity;
                if (claimsIdentity != null)
                {
                    // Ensure TenantId claim exists
                    var tenantIdClaim = claimsIdentity.FindFirst("TenantId");
                    if (tenantIdClaim == null)
                    {
                        // Try alternate claim names
                        var altTenantId = claimsIdentity.FindFirst("tenant_id")
                            ?? claimsIdentity.FindFirst("tenantId");
                        if (altTenantId != null)
                        {
                            claimsIdentity.AddClaim(new System.Security.Claims.Claim("TenantId", altTenantId.Value));
                        }
                    }

                    // Add authentication type
                    claimsIdentity.AddClaim(new System.Security.Claims.Claim("AuthenticationType", "Bearer"));
                }
                return Task.CompletedTask;
            }
        };
    }
})
.AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
    ApiKeyAuthenticationOptions.SchemeName,
    options => { });

// Configure Authorization
builder.Services.AddAuthorization(options =>
{
    // Default policy requires authentication
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .AddAuthenticationSchemes(
            JwtBearerDefaults.AuthenticationScheme,
            ApiKeyAuthenticationOptions.SchemeName)
        .Build();

    // Tenant-based policy (can be used with [Authorize(Policy = "TenantAccess")])
    options.AddPolicy("TenantAccess", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddAuthenticationSchemes(
            JwtBearerDefaults.AuthenticationScheme,
            ApiKeyAuthenticationOptions.SchemeName);
        policy.Requirements.Add(new TenantAuthorizationRequirement(0)); // 0 will be replaced at runtime
    });
});

// Register authorization handlers
builder.Services.AddSingleton<IAuthorizationHandler, TenantAuthorizationHandler>();

// Add controllers for API endpoints
builder.Services.AddControllers();

// Add Document Service Data layer (DbContext, Repositories, UnitOfWork)
builder.Services.AddDocumentServiceData(builder.Configuration);

// Add Document Service Search layer (Meilisearch integration)
builder.Services.AddDocumentServiceSearch(builder.Configuration);

// Add Document Service Processing layer (Inbox Processor Background Service)
builder.Services.AddDocumentServiceProcessing(builder.Configuration);

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
builder.Services.AddScoped<ITenantService, TenantService>();

// Add Document Service (Business Logic Layer)
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<ICollectionService, CollectionService>();
builder.Services.AddScoped<ITemplateService, TemplateService>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured."),
        name: "sql",
        tags: new[] { "db", "sql", "sqlserver" })
    .AddAzureBlobStorage(
        connectionString: builder.Configuration.GetConnectionString("BlobStorage")
            ?? builder.Configuration["BlobStorage:ConnectionString"]
            ?? throw new InvalidOperationException("BlobStorage connection string is not configured."),
        name: "blob-storage",
        tags: new[] { "storage", "blob" })
    .AddCheck<MeilisearchHealthCheck>(
        name: "meilisearch",
        tags: new[] { "search", "meilisearch" });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Enable authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Map health check endpoint
app.MapHealthChecks("/health");

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
