# Elsa.Studio.Authentication.OpenIdConnect.BlazorServer

Blazor Server hosting implementation for OpenID Connect authentication in Elsa Studio.

## Overview

This module provides the Blazor Server-specific implementation for OpenID Connect (OIDC) authentication. It leverages Microsoft's `Microsoft.AspNetCore.Authentication.OpenIdConnect` middleware for robust, production-ready OIDC integration with:

- **Cookie-based authentication** - Secure server-side session management
- **Automatic token refresh** - Silent token renewal using refresh tokens via `OnValidatePrincipal`
- **Token storage in auth properties** - Tokens are stored in the protected authentication ticket and are not exposed to browser JavaScript
- **Standard ASP.NET Core middleware** - Integrates with authentication and authorization pipeline
- **Challenge-based login** - Users are redirected to OIDC provider for authentication

## What You Get

- `ServerTokenProvider` - Retrieves tokens from server-side authentication properties
- `AuthCookieEvents` - Automatic token refresh on every HTTP request
- `TokenRefreshService` - Shared token refresh logic for session and backend API tokens
- `ChallengeToLogin` - Component that initiates OIDC authentication challenge
- `AuthenticationController` - Controller endpoint for manual token refresh (optional)

## Installation

Add a package reference to your Blazor Server project:

```xml
<PackageReference Include="Elsa.Studio.Authentication.OpenIdConnect.BlazorServer" />
```

## Usage

### Basic Setup

In your Blazor Server `Program.cs`:

```csharp
using Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Extensions;

// Configure OpenID Connect authentication
builder.Services.AddOpenIdConnectAuth(options =>
{
    options.Authority = "https://your-identity-server.com";
    options.ClientId = "elsa-studio";
    options.ClientSecret = "your-client-secret"; // Optional for confidential clients
    options.AuthenticationScopes = new[] { "openid", "profile", "elsa_api", "offline_access" };
    options.UsePkce = true; // Recommended
});

// Add authentication middleware (IMPORTANT!)
app.UseAuthentication();
app.UseAuthorization();
```

### Configuration Options

```csharp
builder.Services.AddOpenIdConnectAuth(options =>
{
    // Required
    options.Authority = "https://login.microsoftonline.com/{tenant-id}/v2.0";
    options.ClientId = "your-client-id";

    // Optional
    options.ClientSecret = "your-client-secret"; // For confidential clients
    options.AuthenticationScopes = new[] { "openid", "profile", "offline_access", "api://your-api/scope" };
    options.UsePkce = true; // Default: true
    options.SaveTokens = true; // Default: true (required for token access)
    options.CallbackPath = "/signin-oidc"; // Default
    options.SignedOutCallbackPath = "/signout-callback-oidc"; // Default
    options.GetClaimsFromUserInfoEndpoint = false; // Default: false
    options.RequireHttpsMetadata = true; // Default: true
    options.MetadataAddress = "https://.../.well-known/openid-configuration"; // Auto-discovered if not set
});
```

### Microsoft Entra ID / Azure AD

For Microsoft Entra ID (Azure AD):

```csharp
builder.Services.AddOpenIdConnectAuth(options =>
{
    options.Authority = "https://login.microsoftonline.com/{tenant-id}/v2.0";
    options.ClientId = "{client-id}";
    options.ClientSecret = "{client-secret}";
    options.AuthenticationScopes = new[]
    {
        "openid",
        "profile",
        "offline_access",
        "api://{your-api-id}/elsa-server-api" // Your backend API scope
    };
    options.SaveTokens = true;
});
```

**Important notes for Azure AD:**
- Use the tenant-specific authority URL (not `/common` for production)
- Request `offline_access` to receive refresh tokens
- Only include scopes for ONE resource (your backend API)
- Don't mix Microsoft Graph scopes with custom API scopes (Azure AD limitation)

## Architecture

### Blazor Server-Specific Implementation

#### Cookie-Based Authentication

Blazor Server uses ASP.NET Core cookie authentication:
1. User is redirected to OIDC provider for authentication
2. After successful login, tokens are stored in server-side authentication properties
3. A secure HTTP-only cookie is issued to the browser
4. The cookie contains an encrypted ASP.NET Core authentication ticket
5. Tokens are not accessible to browser JavaScript

**Security benefits:**
- ✅ Tokens stored in a protected authentication ticket
- ✅ HTTP-only, secure cookies
- ✅ No token exposure to JavaScript (XSS protection)
- ✅ Tokens not visible in browser storage/network tools

#### Automatic Token Refresh

Token refresh happens automatically on **every HTTP request** via `AuthCookieEvents.OnValidatePrincipal`:

```csharp
public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
{
    // Get stored access token expiration
    var expiresAt = GetTokenExpiration(context);

    // Check if token is expiring soon (within 2 minutes by default)
    if (expiresAt < DateTimeOffset.UtcNow.Add(RefreshSkew))
    {
        // Refresh the token using refresh token
        var result = await _tokenRefreshService.RefreshTokenAsync(...);

        if (result.Success)
        {
            // Update tokens in authentication properties
            UpdateTokens(context, result);
            context.ShouldRenew = true; // Renew the cookie
        }
        else
        {
            // Refresh failed, reject principal (triggers re-authentication)
            context.RejectPrincipal();
        }
    }
}
```

**How it works:**
1. Every HTTP request triggers `OnValidatePrincipal`
2. Check if access token is expiring soon (within 2 minutes)
3. If yes, call OIDC provider's token endpoint with refresh token
4. Store new tokens in authentication properties
5. Renew the cookie with updated tokens
6. User continues without interruption

**No JavaScript required** - This is the standard ASP.NET Core pattern for server-side token refresh.

#### Token Storage Flow

```
┌─────────────┐      HTTPS       ┌──────────────────┐
│   Browser   │◄────────────────►│  Blazor Server   │
│             │   Cookie only    │                  │
│ No tokens   │                  │ ServerTokenProvider
│             │                  │      │
└─────────────┘                  │      ├─► HttpContext.GetTokenAsync()
                                 │      └─► AuthenticationProperties
                                 │          - access_token
                                 │          - refresh_token
                                 │          - expires_at
                                 └──────────────────┘
```

### Component Structure

```
Elsa.Studio.Authentication.OpenIdConnect/            (Shared core)
├── Contracts/
│   ├── ITokenProvider.cs
│   └── IBackendApiScopeProvider.cs
└── Models/
    └── OidcOptions.cs

Elsa.Studio.Authentication.OpenIdConnect.BlazorServer/ (This module)
├── Controllers/
│   └── AuthenticationController.cs                   (Optional manual refresh endpoint)
├── Services/
│   ├── ServerTokenProvider.cs                        (Token access via HttpContext)
│   ├── TokenRefreshService.cs                        (Shared refresh logic)
│   └── AuthCookieEvents.cs                           (Automatic refresh on request)
├── Components/
│   └── ChallengeToLogin.razor                        (OIDC challenge component)
└── Models/
    └── TokenRefreshResult.cs                         (Refresh result DTO)
```

## Features

### Automatic Silent Token Refresh

Tokens are refreshed automatically on every HTTP request when they're about to expire:

```csharp
// No configuration needed - enabled by default
builder.Services.AddOpenIdConnectAuth(options =>
{
    options.Authority = "...";
    options.ClientId = "...";
    options.AuthenticationScopes = new[] { "openid", "profile", "offline_access" }; // offline_access is key!
});
```

**Prerequisites:**
- `SaveTokens = true` (default)
- Request `offline_access` scope to receive refresh tokens
- OIDC provider issues refresh tokens

### Custom Retry Policy

The token refresh HTTP client uses a default retry policy (3 retries with exponential backoff). You can customize it:

```csharp
builder.Services.AddOpenIdConnectAuth(
    options =>
    {
        options.Authority = "...";
        options.ClientId = "...";
    },
    configureRetryPolicy: () => HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(retryAttempt))
);
```

### Backend API Token Acquisition

When your backend API requires different scopes than the authentication scopes, configure `BackendApiScopes`:

```csharp
builder.Services.AddOpenIdConnectAuth(options =>
{
    options.Authority = "https://your-identity-server.com";
    options.ClientId = "elsa-studio";
    options.AuthenticationScopes = new[] { "openid", "profile", "offline_access" };
    options.BackendApiScopes = new[] { "api://your-api/elsa-server-api" };
});
```

The HTTP message handler automatically requests tokens with backend API scopes when calling the Elsa backend.

## Integration with Elsa Studio

### Complete Setup Example

```csharp
using Elsa.Studio.Authentication.OpenIdConnect.HttpMessageHandlers;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Extensions;
using Elsa.Studio.Core.BlazorServer.Extensions;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using Elsa.Studio.Shell.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add OpenID Connect authentication
builder.Services.AddOpenIdConnectAuth(options =>
{
    options.Authority = "https://login.microsoftonline.com/{tenant-id}/v2.0";
    options.ClientId = "{client-id}";
    options.ClientSecret = "{client-secret}";
    options.AuthenticationScopes = new[] { "openid", "profile", "offline_access" };
    options.BackendApiScopes = new[] { "api://your-api/scope" };
});

// Configure the Elsa backend HTTP client to use OIDC tokens.
var backendApiConfig = new BackendApiConfig
{
    ConfigureBackendOptions = options => options.Url = new("https://your-elsa-api.com/elsa/api"),
    ConfigureHttpClientBuilder = options => options.AuthenticationHandler = typeof(OidcAuthenticatingApiHttpMessageHandler)
};

builder.Services.AddCore();
builder.Services.AddShell();
builder.Services.AddRemoteBackend(backendApiConfig);

var app = builder.Build();

// IMPORTANT: Add authentication middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
```

### HTTP Requests to Elsa Backend

All HTTP requests to the Elsa backend API automatically include access tokens via `OidcAuthenticatingApiHttpMessageHandler`. No manual token handling required.

### SignalR Integration

SignalR connections (for workflow monitoring) automatically receive tokens via `OidcHttpConnectionOptionsConfigurator`. No additional configuration needed.

## Troubleshooting

### "SaveTokens must be true" error

Ensure `SaveTokens = true` in your OIDC configuration (it's the default):

```csharp
options.SaveTokens = true; // Must be true to retrieve tokens later
```

### No refresh token received

Common causes:
1. `offline_access` scope not requested
2. OIDC provider doesn't support refresh tokens
3. Client app not configured for refresh tokens in provider settings
4. Azure AD tenant policies restrict refresh tokens

**Solution:** Ensure `offline_access` is in your scopes:

```csharp
options.AuthenticationScopes = new[] { "openid", "profile", "offline_access" };
```

### Tokens not refreshing automatically

Check that:
1. `SaveTokens = true`
2. Refresh token is present in authentication properties
3. `AuthCookieEvents` is registered (automatic via `AddOpenIdConnectAuth()`)
4. HTTP requests are being made (triggers `OnValidatePrincipal`)

### User redirected to login unexpectedly

This happens when token refresh fails:
1. Refresh token expired or invalid
2. OIDC provider rejected refresh request
3. Network error during refresh

**Solution:** Check server logs for detailed error messages. Users will need to re-authenticate.

### Azure AD: Tokens for wrong audience

Azure AD v2.0 only allows one resource per token request. If you need tokens for multiple APIs:

1. Use authentication scopes for primary authentication
2. Use backend API scopes for Elsa API calls
3. Don't mix Microsoft Graph scopes with custom API scopes

```csharp
// Correct: Separate scopes for authentication vs backend API
options.AuthenticationScopes = new[] { "openid", "profile", "offline_access" };
options.BackendApiScopes = new[] { "api://your-api/elsa-server-api" };
```

## Security Considerations

### Token Security

✅ **Tokens are protected** - Stored in the encrypted authentication ticket and not exposed to browser JavaScript
✅ **HTTP-only cookies** - Not accessible to JavaScript (XSS protection)
✅ **Encrypted cookies** - Cookie content is encrypted by ASP.NET Core
✅ **Secure transmission** - Cookies sent over HTTPS only

### Best Practices

1. **Always use HTTPS** in production (`RequireHttpsMetadata = true`)
2. **Use PKCE** for additional security (`UsePkce = true`)
3. **Request minimal scopes** - Only request what you need
4. **Use client secrets securely** - Store in Key Vault, not in code
5. **Configure cookie security** - Already handled by default configuration
6. **Set appropriate token lifetimes** on your OIDC provider
7. **Monitor refresh failures** - Log and alert on authentication issues

## Related Modules

- **`Elsa.Studio.Authentication.OpenIdConnect`** - Shared OIDC abstractions and models
- **`Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm`** - WebAssembly alternative to this module
- **`Elsa.Studio.Authentication.ElsaIdentity.BlazorServer`** - Elsa Identity (JWT) alternative

## See Also

- [Elsa.Studio.Authentication.OpenIdConnect README](../Elsa.Studio.Authentication.OpenIdConnect/README.md) - Detailed OIDC documentation
- [ASP.NET Core OpenID Connect Documentation](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-api-authorization)
- [Microsoft Entra ID Documentation](https://learn.microsoft.com/en-us/entra/identity-platform/)
