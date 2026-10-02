# Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm

Blazor WebAssembly hosting implementation for OpenID Connect authentication in Elsa Studio.

## Overview

This module provides the Blazor WebAssembly-specific implementation for OpenID Connect (OIDC) authentication. It leverages Microsoft's `Microsoft.AspNetCore.Components.WebAssembly.Authentication` for:

- **Framework-managed authentication** - Uses built-in OIDC flow with PKCE support
- **Automatic token refresh** - Framework handles token expiry and renewal
- **Secure token management** - Tokens managed by framework, not directly exposed
- **Authorization Code Flow with PKCE** - Modern, secure OIDC flow for SPAs
- **Built-in authentication UI** - Standard login/logout pages included

## What You Get

- `WasmTokenProvider` - Retrieves tokens from framework's `IAccessTokenProvider`
- `NavigateToLogin` - Component that redirects to OIDC authentication page
- `Authentication.razor` - Authentication page handling callbacks (`/authentication/{action}`)
- `OpenIdConnectBlazorWasmFeature` - Registers authentication routes and pages
- Automatic token refresh and expiration handling

## Installation

Add a package reference to your Blazor WebAssembly project:

```xml
<PackageReference Include="Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm" />
```

This package references `Microsoft.AspNetCore.Components.WebAssembly.Authentication`, so a separate package reference is normally not needed unless your project manages package references explicitly.

## Usage

### Basic Setup

In your Blazor WebAssembly `Program.cs`:

```csharp
using Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm.Extensions;

// Configure OpenID Connect authentication
builder.Services.AddOpenIdConnectAuth(options =>
{
    options.Authority = "https://your-identity-server.com";
    options.ClientId = "elsa-studio-wasm";
    options.AuthenticationScopes = new[] { "openid", "profile", "elsa_api", "offline_access" };
    options.ResponseType = "code";
});
```

### App.razor Configuration

If your host uses the standard Elsa Studio shell `App` component from `Elsa.Studio.Shell`, you do not need to change `App.razor`. The shell already wraps routes in `CascadingAuthenticationState` and `AuthorizeRouteView`, and it uses the unauthorized component registered by this module.

Only update `App.razor` if you replaced the standard shell router with your own custom app component. In that case, ensure your router uses `CascadingAuthenticationState` and `AuthorizeRouteView`:

```razor
@using Microsoft.AspNetCore.Components.Authorization
@using Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm.Components

<CascadingAuthenticationState>
    <Router AppAssembly="@typeof(App).Assembly">
        <Found Context="routeData">
            <AuthorizeRouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)">
                <NotAuthorized>
                    <!-- NavigateToLogin component provided by this module -->
                    <NavigateToLogin />
                </NotAuthorized>
            </AuthorizeRouteView>
        </Found>
    </Router>
</CascadingAuthenticationState>
```

**Important:** The authentication pages (`/authentication/{action}`) are automatically registered by this module via the `OpenIdConnectBlazorWasmFeature`. You don't need to create an `Authentication.razor` page in your host project.

### index.html Configuration

Blazor WebAssembly OIDC requires Microsoft's authentication JavaScript static asset. The default Elsa Studio WASM host already includes it. If you use a custom host, add this script before `_framework/blazor.webassembly.js`:

```html
<script src="_content/Microsoft.AspNetCore.Components.WebAssembly.Authentication/AuthenticationService.js"></script>
<script src="_framework/blazor.webassembly.js"></script>
```

If this script is missing, the browser console shows an error like:

```text
Could not find 'AuthenticationService.init' ('AuthenticationService' was undefined).
```

### Configuration Options

```csharp
builder.Services.AddOpenIdConnectAuth(options =>
{
    // Required
    options.Authority = "https://login.microsoftonline.com/{tenant-id}/v2.0";
    options.ClientId = "your-client-id";

    // Scopes
    options.AuthenticationScopes = new[] { "openid", "profile", "offline_access", "api://your-api/scope" };

    // OAuth2 settings
    options.ResponseType = "code"; // Default: "code"

    // Callback paths (relative to your app)
    options.CallbackPath = "/authentication/login-callback"; // Default
    options.SignedOutCallbackPath = "/authentication/logout-callback"; // Default

    // Discovery
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
    options.AuthenticationScopes = new[]
    {
        "openid",
        "profile",
        "offline_access",
        "api://{your-api-id}/elsa-server-api" // Your backend API scope
    };
    options.ResponseType = "code";
});
```

**Important notes for Azure AD:**
- Use tenant-specific authority (not `/common` for production)
- Register your app as a "Single Page Application" (SPA) in Azure AD
- Add redirect URI: `https://your-app.com/authentication/login-callback`
- Do not configure a client secret for the SPA client
- **Only include scopes for ONE resource** - Azure AD limitation (don't mix Graph scopes with custom API scopes)
- For userinfo endpoint issues, see troubleshooting section below

## Architecture

### Blazor WebAssembly-Specific Implementation

#### Framework-Managed Authentication

Blazor WebAssembly uses Microsoft's built-in OIDC implementation:

1. User clicks login → Redirected to `/authentication/login`
2. Framework redirects to OIDC provider with Authorization Code Flow + PKCE
3. User authenticates at OIDC provider
4. Provider redirects back to `/authentication/login-callback` with authorization code
5. Framework exchanges code for tokens using PKCE verifier
6. Tokens are stored by the framework (not directly accessible)
7. Framework provides access to tokens via `IAccessTokenProvider`

**Security benefits:**
- ✅ PKCE protection (prevents code interception)
- ✅ Tokens managed by framework
- ✅ Automatic token refresh
- ✅ Standards-compliant OIDC flow

#### Token Management

Unlike Blazor Server, tokens in WebAssembly are handled by the framework:

```
┌─────────────────────┐
│   Browser (WASM)    │
│                     │
│  IAccessTokenProvider
│         │
│         ├─► RequestAccessToken()
│         └─► Token { Value, Expires }
│
│  Tokens stored by
│  framework internally
│  (not directly exposed)
└─────────────────────┘
```

`WasmTokenProvider` wraps `IAccessTokenProvider`:

```csharp
public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
{
    var tokenResult = await _accessTokenProvider.RequestAccessToken();

    if (tokenResult.TryGetToken(out var token))
        return token.Value;

    return null;
}
```

**Automatic token refresh:** The framework automatically refreshes tokens when they're about to expire. You don't need to implement refresh logic.

### Component Structure

```
Elsa.Studio.Authentication.OpenIdConnect/            (Shared core)
├── Contracts/
│   ├── ITokenProvider.cs
│   └── IBackendApiScopeProvider.cs
└── Models/
    └── OidcOptions.cs

Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm/ (This module)
├── Services/
│   └── WasmTokenProvider.cs                         (Token access via IAccessTokenProvider)
├── Components/
│   └── NavigateToLogin.razor                        (Redirect to login page)
├── Pages/
│   └── Authentication.razor                         (Authentication callback page)
└── OpenIdConnectBlazorWasmFeature.cs                (Feature registration)
```

## Features

### Automatic Token Refresh

The framework automatically refreshes tokens when they're about to expire. No configuration needed:

```csharp
// That's it! Token refresh is handled automatically by the framework
builder.Services.AddOpenIdConnectAuth(options =>
{
    options.Authority = "...";
    options.ClientId = "...";
    options.AuthenticationScopes = new[] { "openid", "profile", "offline_access" };
});
```

When a token is about to expire, the framework:
1. Detects expiration via `IAccessTokenProvider`
2. Uses refresh token (if available) to get new access token
3. Updates internal token cache
4. Returns new token to your application

### Authentication Routes

This module provides the required authentication routes automatically:

- `/authentication/login` - Initiates OIDC login flow
- `/authentication/login-callback` - Handles callback from OIDC provider
- `/authentication/logout` - Initiates logout
- `/authentication/logout-callback` - Handles logout callback
- `/authentication/login-failed` - Shown when login fails
- `/authentication/profile` - User profile page (optional)

These routes are registered via `OpenIdConnectBlazorWasmFeature` and use the `RemoteAuthenticatorView` component from Microsoft.

### Programmatic Login/Logout

You can trigger login/logout programmatically via navigation:

```razor
@inject NavigationManager Navigation

<button @onclick="Login">Login</button>
<button @onclick="Logout">Logout</button>

@code {
    void Login() => Navigation.NavigateTo("/authentication/login");
    void Logout() => Navigation.NavigateTo("/authentication/logout");
}
```

Or use the `NavigateToLogin` component provided by this module:

```razor
<NotAuthorized>
    <NavigateToLogin />
</NotAuthorized>
```

## Integration with Elsa Studio

### Complete Setup Example

```csharp
using Elsa.Studio.Authentication.OpenIdConnect.HttpMessageHandlers;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm.Extensions;
using Elsa.Studio.Core.BlazorWasm.Extensions;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using Elsa.Studio.Shell.Extensions;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Add OpenID Connect authentication
builder.Services.AddOpenIdConnectAuth(options =>
{
    options.Authority = "https://login.microsoftonline.com/{tenant-id}/v2.0";
    options.ClientId = "{client-id}";
    options.AuthenticationScopes = new[] { "openid", "profile", "offline_access" };
    options.BackendApiScopes = new[] { "api://your-api/scope" };
    options.CallbackPath = "/authentication/login-callback";
    options.SignedOutCallbackPath = "/authentication/logout-callback";
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

await builder.Build().RunAsync();
```

### HTTP Requests to Elsa Backend

All HTTP requests to the Elsa backend API automatically include access tokens via `OidcAuthenticatingApiHttpMessageHandler`. No manual token handling required.

### SignalR Integration

SignalR connections (for workflow monitoring) automatically receive tokens via `OidcHttpConnectionOptionsConfigurator`. No additional configuration needed.

## Troubleshooting

### Azure AD: "AADSTS28000" - Multi-resource token error

**Error:** Can't request tokens for multiple resources in a single request.

**Cause:** Azure AD v2.0 only allows scopes for ONE resource per token request. You're mixing Microsoft Graph scopes with custom API scopes.

**Solution:** Only include scopes for your backend API:

```csharp
// ❌ Wrong: Mixing Graph and custom API scopes
options.AuthenticationScopes = new[]
{
    "openid", "profile", "offline_access",
    "https://graph.microsoft.com/User.Read",     // Graph scope
    "api://your-api/elsa-server-api"              // Custom API scope
};

// ✅ Correct: Only custom API scopes
options.AuthenticationScopes = new[]
{
    "openid", "profile", "offline_access",
    "api://your-api/elsa-server-api"              // Only custom API scope
};
```

If you need both Graph and custom API tokens, configure them separately:

```csharp
// Authentication scopes (for login)
options.AuthenticationScopes = new[] { "openid", "profile", "offline_access" };

// Backend API scopes (for Elsa API calls)
options.BackendApiScopes = new[] { "api://your-api/elsa-server-api" };
```

### Azure AD: UserInfo endpoint returns 401

**Error:** `graph.microsoft.com/oidc/userinfo: 401 (Unauthorized)`

**Cause:** Azure AD's UserInfo endpoint requires Microsoft Graph permissions.

**Solution 1 - Disable UserInfo endpoint (recommended):**

The shared `OidcOptions` model has `GetClaimsFromUserInfoEndpoint`, but the Blazor WebAssembly module relies on Microsoft's `Microsoft.AspNetCore.Components.WebAssembly.Authentication` pipeline and does not use that server-side OIDC handler option.

For WASM, claims are automatically included in the ID token, so you typically don't need the UserInfo endpoint.

**Solution 2 - Add Graph scope:**

```csharp
options.AuthenticationScopes = new[]
{
    "openid", "profile", "offline_access",
    "https://graph.microsoft.com/User.Read"
};
```

**Note:** This only works if you're NOT using custom API scopes (due to Azure AD's single-resource limitation).

### Login succeeds but redirects to /login-failed

**Common causes:**
1. Token audience doesn't match your backend API's expected audience
2. Required scopes not granted in Azure AD
3. Client ID mismatch between Azure AD registration and configuration
4. Redirect URI not registered in Azure AD

**Solution:**
1. Check browser console for detailed error messages
2. Verify API scope is correctly configured in Azure AD app registration
3. Ensure redirect URIs match exactly (including trailing slashes)
4. Check that backend API validates tokens from correct authority and audience

### CORS errors when calling Elsa backend

Blazor WebAssembly makes direct HTTP requests from the browser, requiring CORS configuration on the Elsa backend:

```csharp
// In Elsa backend API Program.cs
builder.Services.AddCors(cors =>
{
    cors.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins("https://your-blazor-wasm-app.com")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

app.UseCors();
```

### Token refresh not working

**Check:**
1. `offline_access` scope is requested
2. OIDC provider issues refresh tokens
3. Client app is configured for refresh tokens in provider settings

The framework automatically handles refresh, but it requires:
- Refresh token from provider
- Valid refresh token lifetime
- Network connectivity to token endpoint

### Redirect URIs not working

For Azure AD specifically, redirect URIs must be absolute:

```csharp
// Option 1: Let the framework infer absolute URIs (recommended)
options.CallbackPath = "/authentication/login-callback";
// Framework will use current origin: https://your-app.com/authentication/login-callback

// Register the exact absolute URI in your identity provider:
// https://your-app.com/authentication/login-callback
```

## Security Considerations

### Token Storage

Tokens are managed by the framework and not directly exposed to application code. However:

- ⚠️ Tokens are accessible via browser DevTools (Application > IndexedDB)
- ⚠️ Tokens are accessible to JavaScript (XSS risk)
- ✅ Framework uses secure storage mechanisms (IndexedDB, not localStorage)
- ✅ Tokens are never visible in browser history or URL

**Best practices:**
1. Use HTTPS to protect tokens in transit
2. Implement short token lifetimes with refresh tokens
3. Sanitize all user input to prevent XSS attacks
4. Implement Content Security Policy (CSP) headers
5. Keep token lifetimes short

### PKCE Protection

This module uses Authorization Code Flow with PKCE (Proof Key for Code Exchange):

- ✅ Prevents authorization code interception attacks
- ✅ No client secret needed (public client)
- ✅ Standards-compliant OAuth 2.0 for SPAs

### CORS Considerations

Blazor WebAssembly makes cross-origin requests from the browser:

1. Configure CORS on your Elsa backend API
2. Only allow specific origins (don't use `AllowAnyOrigin()` in production)
3. Use `AllowCredentials()` for cookie-based backends
4. Keep CORS configuration as restrictive as possible

## Related Modules

- **`Elsa.Studio.Authentication.OpenIdConnect`** - Shared OIDC abstractions and models
- **`Elsa.Studio.Authentication.OpenIdConnect.BlazorServer`** - Server alternative to this module
- **`Elsa.Studio.Authentication.ElsaIdentity.BlazorWasm`** - Elsa Identity (JWT) alternative

## See Also

- [Elsa.Studio.Authentication.OpenIdConnect README](../Elsa.Studio.Authentication.OpenIdConnect/README.md) - Detailed OIDC documentation
- [Blazor WebAssembly Authentication](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/webassembly/)
- [Microsoft Entra ID for SPAs](https://learn.microsoft.com/en-us/entra/identity-platform/scenario-spa-overview)
- [OAuth 2.0 for Browser-Based Apps (RFC)](https://datatracker.ietf.org/doc/html/draft-ietf-oauth-browser-based-apps)
