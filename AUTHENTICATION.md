# Document Service Authentication and Authorization

This document describes the authentication and authorization implementation for the TaskMaster Document Service.

## Overview

The Document Service implements a dual authentication strategy supporting both JWT Bearer tokens and API key authentication, with tenant-scoped authorization to ensure users can only access resources belonging to their tenant.

## Authentication

### JWT Bearer Authentication

JWT (JSON Web Token) authentication is configured to support token-based authentication for user sessions.

**Configuration** (in `appsettings.json`):
```json
{
  "Jwt": {
    "SecretKey": "your-secret-key-here",
    "ValidIssuer": "TaskMaster.DocumentService",
    "ValidAudience": "TaskMaster.DocumentService.Api",
    "RequireHttpsMetadata": true,
    "ValidateLifetime": true,
    "ClockSkewMinutes": 5
  }
}
```

**Required Claims**:
- `TenantId`: The tenant ID associated with the user
- `TenantName` (optional): The tenant name
- Additional standard JWT claims (issuer, audience, expiration)

**Usage**:
```bash
curl -H "Authorization: Bearer YOUR_JWT_TOKEN" https://api.example.com/api/tenants/1
```

### API Key Authentication

API key authentication allows service-to-service communication and API clients to authenticate using a pre-shared key.

**Configuration** (in `appsettings.json`):
```json
{
  "ApiKey": {
    "HeaderName": "X-API-Key",
    "Keys": {
      "your-api-key-123": {
        "TenantId": 1,
        "TenantName": "Organization Name",
        "Description": "Production API key",
        "IsActive": true
      }
    }
  }
}
```

**Usage**:
```bash
curl -H "X-API-Key: your-api-key-123" https://api.example.com/api/tenants/1
```

## Authorization

### Tenant-Scoped Authorization

The service implements tenant-scoped authorization to ensure users can only access resources belonging to their tenant.

#### TenantAuthorizationAttribute

Apply this attribute to controllers or actions to enforce tenant-scoped access:

```csharp
[HttpGet("{tenantId}")]
[TenantAuthorization("tenantId")] // Parameter name in route
public async Task<IActionResult> GetTenant(int tenantId)
{
    // User can only access if their TenantId claim matches the tenantId parameter
}
```

**How it works**:
1. Extracts the tenant ID from the user's claims
2. Extracts the requested tenant ID from route or query parameters
3. Compares the two values
4. Returns 403 Forbidden if they don't match

#### Authorization Policies

The service defines the following authorization policies:

- **Default Policy**: Requires authentication via JWT or API Key
- **TenantAccess Policy**: Requires authentication and tenant-scoped access

## Implementation Details

### File Structure

```
src/TaskMaster.DocumentService.Api/
├── Authentication/
│   ├── JwtOptions.cs                    # JWT configuration options
│   ├── ApiKeyOptions.cs                 # API key configuration options
│   └── ApiKeyAuthenticationHandler.cs   # API key authentication handler
├── Authorization/
│   ├── TenantAuthorizationAttribute.cs  # Tenant-scoped authorization filter
│   └── TenantAuthorizationHandler.cs    # Authorization handler for policies
├── Extensions/
│   └── ClaimsPrincipalExtensions.cs     # Helper methods for claims
└── Controllers/
    └── TenantsController.cs             # Example authenticated controller
```

### Configuration in Program.cs

Authentication and authorization are configured in `Program.cs:18-110`:

```csharp
// Configure Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options => { /* JWT configuration */ })
.AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>("ApiKey", options => { });

// Configure Authorization
builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme, "ApiKey")
        .Build();
});
```

## Security Best Practices

1. **Secret Management**:
   - Store JWT secret keys and API keys in Azure Key Vault or similar secure storage
   - Never commit secrets to source control
   - Use environment variables or configuration providers for production

2. **HTTPS**:
   - Always use HTTPS in production (`RequireHttpsMetadata: true`)
   - Enforce HTTPS redirects

3. **Token Validation**:
   - Validate token lifetime
   - Validate issuer and audience
   - Use appropriate clock skew settings

4. **API Key Rotation**:
   - Regularly rotate API keys
   - Use the `IsActive` flag to disable compromised keys without removing them
   - Include descriptions to track API key usage

5. **Logging**:
   - Log authentication failures
   - Log authorization failures with tenant context
   - Monitor for suspicious patterns

## Testing

Comprehensive tests are included in `tests/TaskMaster.DocumentService.Api.Tests/`:

- **Authentication Tests**: 6 test cases covering API key authentication
- **Authorization Tests**: 14 test cases covering tenant authorization
- **Extension Tests**: 11 test cases for claims helper methods
- **Controller Tests**: 8 test cases for authenticated endpoints

Run tests:
```bash
dotnet test tests/TaskMaster.DocumentService.Api.Tests/
```

## Example Usage

### Protecting an Endpoint

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize] // Requires JWT or API Key authentication
public class DocumentsController : ControllerBase
{
    [HttpGet("{tenantId}/documents")]
    [TenantAuthorization("tenantId")] // Enforces tenant-scoped access
    public async Task<IActionResult> GetDocuments(int tenantId)
    {
        // User's TenantId must match the requested tenantId
        var documents = await _service.GetDocumentsAsync(tenantId);
        return Ok(documents);
    }
}
```

### Accessing User Context

```csharp
using TaskMaster.DocumentService.Api.Extensions;

public class MyController : ControllerBase
{
    public IActionResult MyAction()
    {
        var tenantId = User.GetTenantId();
        var tenantName = User.GetTenantName();
        var authType = User.GetAuthenticationType(); // "Bearer" or "ApiKey"

        if (!User.HasAccessToTenant(requestedTenantId))
        {
            return Forbid();
        }

        // ...
    }
}
```

## Troubleshooting

### Authentication Issues

1. **401 Unauthorized**:
   - Check that Authorization header is present
   - Verify JWT token or API key is valid
   - Check token expiration

2. **403 Forbidden**:
   - Verify user has a TenantId claim
   - Check that TenantId matches the requested resource
   - Verify API key is active

3. **500 Internal Server Error**:
   - Check JWT configuration (secret key, issuer, audience)
   - Verify API key configuration is loaded correctly
   - Check application logs for details

## Future Enhancements

Potential improvements for future iterations:

1. **Role-Based Authorization**: Add role claims for fine-grained access control
2. **Hierarchical Tenants**: Support parent-child tenant access patterns
3. **OAuth 2.0**: Add support for OAuth 2.0 authorization code flow
4. **Rate Limiting**: Implement rate limiting per tenant/API key
5. **Audit Logging**: Enhanced audit logging for compliance
6. **API Key Management**: Admin endpoints for API key CRUD operations
