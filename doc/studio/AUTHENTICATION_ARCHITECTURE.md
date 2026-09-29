# Elsa Studio Authentication Architecture

This document provides an overview of the authentication architecture in Elsa Studio, including how different authentication providers integrate with the framework.

## Overview

Elsa Studio supports multiple authentication providers through a flexible, extensible architecture. The system is designed to:

1. Support multiple authentication mechanisms (brokered External Authentication, direct OIDC, Elsa Identity JWT)
2. Work across different Blazor hosting models (Server and WebAssembly)
3. Provide automatic token management and refresh
4. Integrate seamlessly with backend API calls and SignalR connections
5. Present a single, provider-neutral, themeable sign-in surface at `/login`

A Studio host activates **exactly one** provider. The choice is deployment configuration, not a code branch that can silently drift:

| `Authentication:Provider` | Module | Trust model |
|---|---|---|
| `ExternalAuthentication` | `Elsa.Studio.ExternalAuthentication.*` | Elsa Server acts as the broker; providers are managed at runtime. **Recommended for new deployments.** |
| `OpenIdConnect` | `Elsa.Studio.Authentication.OpenIdConnect.*` | Studio talks directly to the upstream IdP; providers are deployment configuration. |
| `ElsaIdentity` | `Elsa.Studio.Authentication.ElsaIdentity.*` | Username/password against the Elsa backend, JWT access/refresh tokens. |
| `ElsaLogin` | `Elsa.Studio.Login` (deprecated) | Legacy login page and provider manager. Retained for compatibility only. |

## Provider selection and startup validation

Each provider module records a `StudioAuthenticationProviderRegistration`. The host declares its intent separately:

```csharp
builder.Services.AddStudioAuthenticationMode(options => options.Provider = selectedAuthProvider);
```

`StudioAuthenticationOptionsValidator` runs with `ValidateOnStart()` and fails the host when:

- more than one provider's handlers are registered (mixed trust models),
- no provider is registered at all, or
- the registered provider does not match `Authentication:Provider`.

This makes an ambiguous authentication configuration a startup error rather than a runtime surprise. In particular, direct OpenID Connect and brokered External Authentication must never be enabled together.

## Architecture layers

```
┌─────────────────────────────────────────────────────────────────┐
│                     Elsa Studio Application                      │
│  (Workflows, Dashboard, Security, etc.)                          │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│              Elsa.Studio.Authentication.UI                       │
│  • Owns /login, the login panel, and the theme framework         │
│  • Provider-neutral: renders whatever methods are contributed    │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│           Elsa.Studio.Authentication.Abstractions                │
│  • ILoginMethodCatalog / ComponentProvider / IconProvider        │
│  • IHttpConnectionOptionsConfigurator (SignalR)                  │
│  • StudioAuthenticationOptions + mutual-exclusion validation     │
├─────────────────────────────────────────────────────────────────┤
│                    Elsa.Studio.Core                              │
│  • ISingleFlightCoordinator - Prevents concurrent token refresh  │
└──────────────────────────┬──────────────────────────────────────┘
                           │
      ┌────────────────────┼────────────────────┬────────────────┐
      ▼                    ▼                    ▼                ▼
┌──────────────┐  ┌──────────────────┐  ┌──────────────┐  ┌───────────┐
│ External     │  │ OpenIdConnect    │  │ ElsaIdentity │  │ Login     │
│ Authentication│ │ (direct)         │  │              │  │(deprecated)│
│ ──────────── │  │ ──────────────── │  │ ──────────── │  │ ───────── │
│ • Broker at  │  │ • ITokenProvider │  │ • JWT tokens │  │ • Legacy  │
│   Elsa Server│  │ • OidcOptions    │  │ • Auto refresh│ │   /login  │
│ • Runtime    │  │ • Server & WASM  │  │ • Server &   │  │           │
│   connections│  │ • Auto refresh   │  │   WASM       │  │           │
│ • Server &   │  │                  │  │              │  │           │
│   WASM       │  │                  │  │              │  │           │
└──────────────┘  └──────────────────┘  └──────────────┘  └───────────┘
```

## The shared login surface

`Elsa.Studio.Authentication.UI` owns the `/login` route for every provider except the deprecated `Elsa.Studio.Login` package. It is registered once in the composition root:

```csharp
builder.Services
    .AddAuthenticationUI(configuration.GetSection(LoginThemeOptions.SectionName))
    .AddElsaStudioLoginThemes();   // optional extra themes
```

Providers contribute *content*; the shell owns *behavior*. The seam lives in `Elsa.Studio.Authentication.Abstractions`:

| Contract | Purpose |
|---|---|
| `ILoginMethodCatalog` | Lists safe, non-sensitive presentation metadata for the enabled sign-in methods. |
| `ILoginMethodComponentProvider` | Maps a method `Kind` (`local`, `external`, `elsa-identity`, `direct-openid-connect`) to a Blazor component. |
| `ILoginMethodIconProvider` | Contributes trusted, locally supplied SVG icons referenced by `IconId`. |
| `LoginMethodDescriptor` | `Id`, `Key`, `Kind`, `DisplayName`, `IconId`, `Order`, `IsPreferred`, `InitiationUri`. |
| `LoginFailureCodes` | Fixed, non-sensitive failure codes (currently `sign_in_failed`) surfaced to the login UI. |

`LoginPanel` aggregates every registered catalog, orders methods, separates local from external methods, and renders each through its registered component. Rules the shell enforces regardless of provider:

- A preferred method is **visual emphasis only** — login never redirects automatically; the user always takes an explicit action.
- Return paths are normalized to client-local absolute paths; `//`, backslashes, and absolute URLs collapse to `/`.
- A catalog that throws degrades to a warning (or an error if no methods loaded at all) rather than breaking the page.
- Errors arrive as fixed codes in the query string, never as provider text.

Theme selection is a stable ID read at startup (`Authentication:Login:Theme`, default `classic`). See the [Authentication UI README](../src/modules/Elsa.Studio.Authentication.UI/README.md) for the theme contract and CSS token list.

## External Authentication (broker)

This is the newest and recommended integration. Instead of Studio holding upstream IdP configuration, **Elsa Server acts as the broker**: it owns the identity provider connections, performs the upstream protocol exchange, and hands Studio an Elsa access/refresh token pair. Providers can then be added, edited, and disabled at runtime through the Studio administration UI rather than through Studio deployment configuration.

Three packages are involved:

| Package | Role |
|---|---|
| `Elsa.Studio.ExternalAuthentication` | Host-neutral: broker Refit contracts, login-method contributions, and the administration UI (connections, identity links, sessions). |
| `Elsa.Studio.ExternalAuthentication.BlazorServer` | Confidential client. Code exchange server-side, credentials in a server-side ticket store, HTTP-only cookie in the browser. |
| `Elsa.Studio.ExternalAuthentication.BlazorWasm` | Public client. Browser PKCE, memory-only credentials by default. |

The administration module (`AddExternalAuthenticationModule`) is registered independently of the login provider and remains gated by the Elsa backend's feature and permission checks — a host can manage broker connections while still signing in with a different provider.

### Broker endpoints

Studio consumes a small, frozen REST surface on the Elsa backend:

| Endpoint | Used for |
|---|---|
| `GET /external-authentication/login-methods?clientId=…` | Anonymous discovery of enabled methods. Presentation data only. |
| `GET /external-authentication/authorize/{connectionKey}` | Starts an upstream authorization flow. |
| `POST /external-authentication/local/authorize` | Broker-local username/password flow; returns a callback redirect URI. |
| `POST /external-authentication/token` | Exchanges an authorization code or rotates a refresh token (form-encoded). |
| `POST /external-authentication/logout` | Local or upstream sign-out; may return a continuation URL. |

Discovery, code exchange, and refresh deliberately resolve through `IAnonymousBackendApiClientProvider` rather than the authenticated Refit client — routing them through the bearer handler would create a sign-in/refresh recursion.

Both hosts register the fixed callback paths. Custom values are rejected at startup:

```
/authentication/external/callback
/authentication/external/logout-callback
```

### Blazor Server (confidential client)

```
┌─────────────────────────────────────────────────────────────────┐
│  /login → LoginPanel → ExternalAuthenticationLoginMethodCatalog  │
│  (anonymous discovery against the broker)                        │
└──────────────────────────┬──────────────────────────────────────┘
                           │ user picks a method
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│         ExternalAuthenticationController (server-side)           │
│  • GET  /authentication/external/login/{connectionKey}          │
│  • POST /authentication/external/local-login  (antiforgery)     │
│  • Generates code_verifier + state server-side                  │
│  • Stores them in a data-protected, HTTP-only, 10-min cookie    │
└──────────────────────────┬──────────────────────────────────────┘
                           │ redirect to the broker
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│        GET /authentication/external/callback                     │
│  • Takes and deletes the transaction cookie (single use)        │
│  • Fixed-time state comparison                                  │
│  • POST /external-authentication/token with Basic client auth   │
│  • Builds a principal from the access-token claims              │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│          ServerExternalAuthenticationTicketStore                 │
│  • Ticket (incl. refresh token) stays in server memory cache    │
│  • Browser receives only an opaque session key cookie           │
│  • Ticket lifetime = min(refresh expiry, external session expiry)│
└─────────────────────────────────────────────────────────────────┘
```

**Token acquisition and refresh** (`ServerExternalAuthenticationStateProvider`, which is both the `AuthenticationStateProvider` and the `IExternalAuthenticationTokenProvider`):

```
GetAccessTokenAsync()
    → Not authenticated? return null
    → Read access_token / access_expires_at from the ticket
    → Not within 2 min of expiry? return access_token
    → ServerExternalAuthenticationRefreshCoordinator.RunAsync(SHA-256(refresh_token), …)
        → POST /external-authentication/token (grant_type=refresh_token, Basic auth)
        → Re-issue the ticket with the rotated credentials
    → On failure: SignOut and return null
```

The refresh coordinator keys single-flight work on a **hash of the refresh token**, so every concurrent request that observed the same rotating credential shares one exchange instead of replaying a single-use token.

Configuration:

```csharp
builder.Services.AddExternalAuthenticationBroker(options =>
    configuration.GetSection("Authentication:ExternalAuthentication").Bind(options));
```

```json
{
  "Authentication": {
    "Provider": "ExternalAuthentication",
    "ExternalAuthentication": {
      "ClientId": "elsa-studio-server",
      "ClientSecret": "{deployment-secret}",
      "CallbackPath": "/authentication/external/callback",
      "LogoutCallbackPath": "/authentication/external/logout-callback"
    }
  }
}
```

`ClientId` and `ClientSecret` are both required — Studio Server refuses to start as a public client. Keep the secret out of source-controlled configuration.

The sample Server host therefore defaults to `ElsaIdentity`. To opt into the broker without changing checked-in configuration, provide the provider and secret through environment variables, for example:

```bash
Authentication__Provider=ExternalAuthentication \
Authentication__ExternalAuthentication__ClientSecret="<deployment-secret>" \
dotnet run --project src/hosts/Elsa.Studio.Host.Server
```

Cookie: `ElsaStudio.ExternalAuthentication`, HTTP-only, `Secure` always, `SameSite=Lax`, non-sliding, 8-hour lifetime, backed by the server-side ticket store.

### Blazor WebAssembly (public client)

```
┌─────────────────────────────────────────────────────────────────┐
│  /login → LoginPanel → ExternalAuthenticationLoginMethodCatalog  │
└──────────────────────────┬──────────────────────────────────────┘
                           │ user picks a method
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│        ExternalAuthenticationWasmLoginCoordinator                │
│  • BrowserExternalAuthenticationPkceService → Web Crypto API    │
│    (state, code_verifier, S256 code_challenge)                  │
│  • Transaction saved by the PKCE transaction store (10 min)     │
│  • InitiationUri validated to the configured backend origin     │
│  • redirect_uri must be this Studio origin's exact callback     │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│  /authentication/external/callback (ExternalAuthenticationCallback)│
│  • Exact scheme/host/port/path match required                   │
│  • State taken (and consumed) BEFORE errors are honored,        │
│    which prevents callback replay                               │
│  • POST /external-authentication/token with code + verifier     │
│  • Tokens handed to ExternalAuthenticationWasmTokenProvider     │
└─────────────────────────────────────────────────────────────────┘
```

**Token acquisition and refresh** (`ExternalAuthenticationWasmTokenProvider`):

```
GetAccessTokenAsync()
    → No token set? return null
    → Access token healthy (>1 min left, session not expired)? return it
    → Take the refresh lock, re-read (another caller may have rotated already)
    → External session expired or refresh token expired? clear and return null
    → POST /external-authentication/token (grant_type=refresh_token, no client auth)
    → Store rotated set, raise TokensChanged
    → On any failure: clear the local credentials (rotation reuse / revocation)
```

Lifetimes are clamped: the access and refresh expiries can never outlive the broker's reported external-session expiry.

Configuration:

```csharp
builder.Services.AddExternalAuthenticationBroker(options =>
    configuration.GetSection("Authentication:ExternalAuthentication").Bind(options));
```

```json
{
  "Authentication": {
    "Provider": "ExternalAuthentication",
    "ExternalAuthentication": {
      "ClientId": "elsa-studio-wasm",
      "CallbackPath": "/authentication/external/callback",
      "LogoutCallbackPath": "/authentication/external/logout-callback",
      "BrowserStorage": "Memory"
    }
  }
}
```

A configured `ClientSecret` is a startup error — WebAssembly is a public client.

| `BrowserStorage` | Behavior |
|---|---|
| `Memory` (default) | Credentials live only in the running app. Reload, new tab, or tab close signs the user out. |
| `Session` | Tab-scoped session storage. Emits a startup security warning and a visible login-panel warning. |
| `Durable` | Local storage, survives the browser session. Emits a startup security warning and a visible login-panel warning. |

Both persistent modes increase exposure to browser-script compromise. The warning text is surfaced through `LoginMethodCatalogResult.SecurityWarning` so users see it on the sign-in surface, not just in server logs.

### Logout

Both hosts support `local` and `upstream` sign-out modes, and in both cases **local sign-out never depends on upstream availability or network success**.

- **Server**: `POST /authentication/external/logout` (authenticated, antiforgery-validated) calls the broker, signs out of the cookie scheme, and — when the broker reports an incomplete upstream flow — stores a `logout` transaction and redirects to the broker-supplied navigation URL. `GET /authentication/external/logout-callback` consumes that transaction and returns the user to the validated local path.
- **WASM**: `ExternalAuthenticationWasmLogoutService` calls the broker, clears local credentials in a `finally` block, and only then follows a continuation URL — which must be same-origin with the configured backend and contain `/external-authentication/logout/continue/`.

### Administration UI

`Elsa.Studio.ExternalAuthentication` also contributes the management surface under the Security menu:

| Route | Purpose |
|---|---|
| `/security/external-authentication/connections` | Identity provider connection management. |
| `/security/external-authentication/identity-links` | Tenant-scoped prelink and unlink operations. |
| `/security/external-authentication/sessions` | Session list and revocation. |

Menus and buttons are permission-gated usability affordances — Elsa API authorization remains authoritative. Connections supplied by deployment configuration are visible but read-only; a privileged admin may create an explicit Studio override, which deliberately clears all secret bindings so they must be reconfigured. Secret values are never displayed, cloned, or returned; a managed secret value travels only to the dedicated write-only replacement endpoint. Connection keys are immutable after creation, and provider callback URIs are derived by Elsa from deployment-owned public-origin configuration.

See the [module README](../src/modules/Elsa.Studio.ExternalAuthentication/README.md) for the full management contract.

## OpenID Connect Provider (Blazor Server)

### Architecture

The OIDC Blazor Server implementation follows the standard ASP.NET Core pattern with automatic token refresh:

```
┌─────────────────────────────────────────────────────────────────┐
│                    HTTP Request                                  │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│              Cookie Authentication Middleware                    │
│  • Validates cookie on every request                            │
│  • Triggers OnValidatePrincipal event                           │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                    AuthCookieEvents                              │
│  • Checks if access_token is expiring (within 2 min skew)       │
│  • If expiring: calls TokenRefreshService                       │
│  • Updates tokens in cookie (context.ShouldRenew = true)        │
│  • If refresh fails: rejects principal → re-authentication      │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                   TokenRefreshService                            │
│  • Reads OpenIdConnectOptions (client ID, secret)               │
│  • Discovers token endpoint from OIDC metadata                  │
│  • Performs OAuth2 refresh_token grant                          │
│  • Returns TokenRefreshResult (access_token, expires_at)        │
│  • HTTP client has Polly retry policy (configurable)            │
└─────────────────────────────────────────────────────────────────┘
```

### Key Components

| Component | Purpose |
|-----------|---------|
| `TokenRefreshService` | Core OAuth2 refresh token grant logic. Shared between session refresh and backend API token acquisition. |
| `AuthCookieEvents` | Cookie authentication events that automatically refresh tokens via `OnValidatePrincipal`. Standard ASP.NET Core pattern. |
| `ServerTokenProvider` | Implements `ITokenProvider`. Returns cookie's access token, or acquires scope-specific tokens for backend API calls. |
| `OidcOptions` | Configuration: Authority, ClientId, Scopes, BackendApiScopes, etc. |
| `TokenRefreshResult` | Simple result record: Success, AccessToken, RefreshToken, ExpiresAt. |
| `DirectOpenIdConnectLoginMethodCatalog` | Contributes a single "Single sign-on" method to the shared `/login` shell. |

### Token Flow

**Session Token Refresh (automatic):**
```
HTTP Request → Cookie Middleware → AuthCookieEvents.OnValidatePrincipal
    → Check expires_at (2 min skew)
    → TokenRefreshService.RefreshTokenAsync(refreshToken)
    → Update cookie tokens → Continue request
```

**Backend API Token (on-demand):**
```
ServerTokenProvider.GetAccessTokenAsync()
    → If no BackendApiScopes: return cookie's access_token
    → If BackendApiScopes configured:
        → Check in-memory cache
        → TokenRefreshService.RefreshTokenAsync(refreshToken, scopes)
        → Cache result → Return access_token
```

### Configuration

```csharp
// Program.cs
builder.Services.AddOpenIdConnectAuth(options =>
{
    options.Authority = "https://login.microsoftonline.com/{tenant}/v2.0";
    options.ClientId = "your-client-id";
    options.ClientSecret = "your-client-secret"; // Optional for confidential clients
    options.AuthenticationScopes = ["openid", "profile", "offline_access"];

    // Optional: Different scopes for backend API (multi-audience scenarios)
    options.BackendApiScopes = ["api://backend-api/.default"];
});

// Custom retry policy (optional)
builder.Services.AddOpenIdConnectAuth(
    options => { /* ... */ },
    configureRetryPolicy: () => HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(5, _ => TimeSpan.FromSeconds(1)));

app.UseAuthentication();
app.UseAuthorization();
```

Callback paths default to `/signin-oidc` and `/signout-callback-oidc` when not specified.

### Default Retry Policy

The `TokenRefreshService` HTTP client has a configurable Polly retry policy:

- **Default**: 3 retries with exponential backoff (1s, 2s, 4s)
- **Handles**: Transient HTTP errors (5xx, 408) and 429 Too Many Requests
- **Customizable**: Pass `configureRetryPolicy` to `AddOpenIdConnectAuth()`

## OpenID Connect Provider (Blazor WebAssembly)

Uses Microsoft's built-in `Microsoft.AspNetCore.Components.WebAssembly.Authentication`:

- Tokens managed by browser-based authentication framework
- Accessed via `IAccessTokenProvider`
- Automatic token refresh before expiry
- Secure token storage in browser

## ElsaIdentity Provider

For Elsa Identity (username/password authentication against Elsa backend).

### Architecture

The ElsaIdentity provider uses JWT tokens stored in browser storage (WASM) or server-side session (Server), with automatic token refresh:

```
┌─────────────────────────────────────────────────────────────────┐
│                 API Call / SignalR Connection                    │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                  JwtAuthenticationProvider                       │
│  • Implements IAuthenticationProvider                           │
│  • Reads access token from IJwtAccessor                         │
│  • Checks if token is expiring (within 2 min skew)              │
│  • If expiring: calls IRefreshTokenService (single-flight)      │
│  • If refresh fails: clears all tokens → unauthenticated        │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                   IRefreshTokenService                           │
│  • Calls Elsa backend's refresh endpoint                        │
│  • Returns IsAuthenticated + new tokens                         │
│  • Tokens stored via IJwtAccessor                               │
└─────────────────────────────────────────────────────────────────┘
```

### Key Components

| Component | Purpose |
|-----------|---------|
| `JwtAuthenticationProvider` | Implements `IAuthenticationProvider`. Gets access tokens with automatic refresh before expiry. |
| `IRefreshTokenService` | Calls Elsa backend to refresh tokens. |
| `IJwtAccessor` | Reads/writes JWT tokens (LocalStorage for WASM, session for Server). |
| `IJwtParser` | Parses JWT to extract expiry claim. |
| `ElsaIdentityLoginMethodCatalog` | Contributes the `elsa-identity` credential method to the shared `/login` shell. |

### Token Flow

```csharp
public class JwtAuthenticationProvider : IAuthenticationProvider
{
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromMinutes(2);

    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var accessToken = await jwtAccessor.ReadTokenAsync("accessToken");

        if (string.IsNullOrWhiteSpace(accessToken))
            return null;

        if (!IsExpiredOrNearExpiry(accessToken))
            return accessToken;

        // Single-flight refresh via ISingleFlightCoordinator
        var refreshResponse = await refreshCoordinator.RunAsync(
            refreshTokenService.RefreshTokenAsync, cancellationToken);

        if (!refreshResponse.IsAuthenticated)
        {
            // Clear all tokens on refresh failure
            await jwtAccessor.ClearTokenAsync("accessToken");
            await jwtAccessor.ClearTokenAsync("refreshToken");
            await jwtAccessor.ClearTokenAsync("idToken");
            return null;
        }

        return await jwtAccessor.ReadTokenAsync("accessToken");
    }
}
```

### Configuration

```csharp
// Blazor Server and Blazor WASM
builder.Services.AddElsaIdentity();
builder.Services.AddElsaIdentityUI();
```

## Legacy Elsa.Studio.Login (deprecated)

`Elsa.Studio.Login` keeps its own legacy `/login` page and is selected with `Authentication:Provider = "ElsaLogin"`. It must be used **by itself** — do not register it alongside `Elsa.Studio.Authentication.UI` or any broker package; startup validation rejects mixed registrations.

To migrate a legacy host: select `ElsaIdentity` or `ExternalAuthentication`, remove `AddLoginModule()`, and register the shared authentication UI in the composition root.

## Integration Points

### 1. API Calls

Each provider supplies its own `DelegatingHandler`, which the host wires into `BackendApiConfig.ConfigureHttpClientBuilder`:

| Provider | Handler |
|---|---|
| External Authentication (Server) | `ExternalAuthentication.BlazorServer.HttpMessageHandlers.ExternalAuthenticationAuthenticatingApiHttpMessageHandler` |
| External Authentication (WASM) | `ExternalAuthentication.BlazorWasm.HttpMessageHandlers.ExternalAuthenticationAuthenticatingApiHttpMessageHandler` |
| OpenID Connect | `OidcAuthenticatingApiHttpMessageHandler` |
| Elsa Identity | `ElsaIdentityAuthenticatingApiHttpMessageHandler` |
| Login (deprecated) | `AuthenticatingApiHttpMessageHandler` |

```csharp
var backendApiConfig = new BackendApiConfig
{
    ConfigureBackendOptions = options => configuration.GetSection("Backend").Bind(options),
    ConfigureHttpClientBuilder = options => options.AuthenticationHandler = authenticationHandler,
};
```

The broker handler resolves the token provider per request and attaches the current access token:

```csharp
public sealed class ExternalAuthenticationAuthenticatingApiHttpMessageHandler(IBlazorServiceAccessor blazorServiceAccessor) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var tokenProvider = blazorServiceAccessor.Services.GetRequiredService<IExternalAuthenticationTokenProvider>();
        var accessToken = await tokenProvider.GetAccessTokenAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(accessToken))
            request.Headers.Authorization = new("Bearer", accessToken);

        return await base.SendAsync(request, cancellationToken);
    }
}
```

Because the token provider refreshes on demand, the handler itself stays free of refresh logic.

### 2. SignalR Connections

SignalR connections are authenticated via `IHttpConnectionOptionsConfigurator`, which is defined in `Elsa.Studio.Authentication.Abstractions` and implemented by each authentication provider.

**Interface** (`Elsa.Studio.Authentication.Abstractions`):
```csharp
public interface IHttpConnectionOptionsConfigurator
{
    Task ConfigureAsync(HttpConnectionOptions options, CancellationToken cancellationToken = default);
}
```

**Implementations:**

| Provider | Configurator |
|---|---|
| External Authentication (Server) | `ExternalAuthenticationServerHttpConnectionOptionsConfigurator` |
| External Authentication (WASM) | `ExternalAuthenticationHttpConnectionOptionsConfigurator` |
| OpenID Connect | `OidcHttpConnectionOptionsConfigurator` |
| Elsa Identity | `ElsaIdentityHttpConnectionOptionsConfigurator` |

**OIDC Implementation** (`Elsa.Studio.Authentication.OpenIdConnect`):
```csharp
public class OidcHttpConnectionOptionsConfigurator(ITokenProvider tokenProvider) : IHttpConnectionOptionsConfigurator
{
    public Task ConfigureAsync(HttpConnectionOptions options, CancellationToken cancellationToken = default)
    {
        options.AccessTokenProvider = async () => await tokenProvider.GetAccessTokenAsync(cancellationToken);
        return Task.CompletedTask;
    }
}
```

**Usage in WorkflowInstanceObserverFactory** (`Elsa.Studio.Workflows`):
```csharp
public class WorkflowInstanceObserverFactory(
    // ...
    IHttpConnectionOptionsConfigurator httpConnectionOptionsConfigurator)
{
    public async Task<IWorkflowInstanceObserver> CreateAsync(WorkflowInstanceObserverContext context)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                // Delegates to the provider-specific configurator.
                httpConnectionOptionsConfigurator.ConfigureAsync(options, cancellationToken).GetAwaiter().GetResult();
            })
            .Build();
        // ...
    }
}
```

### 3. Unauthorized UI

Each provider registers `UnauthorizedComponentProvider<TComponent>`:

| Provider | Unauthorized component |
|---|---|
| External Authentication (Server) | `ChallengeToExternalLogin` |
| External Authentication (WASM) | `NavigateToExternalLogin` |
| OpenID Connect (Server) | `ChallengeToLogin` |
| OpenID Connect (WASM) | `NavigateToLogin` |
| Elsa Identity | `Unauthorized` / `RedirectToLogin` |

## Security Considerations

### External Authentication (Blazor Server)

- ✅ Confidential client — code exchange happens server-side with Basic client authentication
- ✅ Refresh credentials stay in a server-side ticket store; the browser holds only an opaque session key
- ✅ Cookie: HttpOnly, Secure, SameSite=Lax, non-sliding, 8-hour lifetime
- ✅ PKCE verifier and state generated server-side and stored in a data-protected, single-use, 10-minute cookie
- ✅ Fixed-time state comparison on callback
- ✅ Ticket lifetime bounded by the broker's refresh and external-session expiry
- ✅ Single-flight refresh keyed on the refresh-token hash prevents single-use-token replay
- ✅ Antiforgery validation on the local-login and logout POST endpoints
- ✅ Local credentials travel in a POST body, never in the address bar
- ✅ Refresh failure signs the user out

### External Authentication (Blazor WebAssembly)

- ✅ Public client with **mandatory** S256 PKCE via the browser Web Crypto API; a configured client secret is a startup error
- ✅ Memory-only credentials by default
- ✅ Callback URI validated for exact scheme, host, port, and path
- ✅ Broker-supplied initiation and logout-continuation URIs validated against the configured backend origin
- ✅ Callback state consumed before errors are honored, preventing replay
- ✅ Access/refresh lifetimes clamped to the broker's external-session expiry
- ✅ Failed refresh clears local credentials (assumes rotation reuse or revocation)
- ⚠️ `Session` / `Durable` browser storage is opt-in, emits a startup warning, and shows a warning on the login panel

### Blazor Server (OIDC)

- ✅ Tokens stored server-side in authentication cookie properties
- ✅ Cookie: HttpOnly, Secure, SameSite=Lax
- ✅ 8-hour cookie expiration with sliding window
- ✅ Automatic token refresh via `OnValidatePrincipal` (no JavaScript needed)
- ✅ PKCE enabled by default
- ✅ Token refresh failures force re-authentication

### Blazor WebAssembly (OIDC)

- ✅ Tokens managed by Microsoft's authentication framework
- ✅ Automatic token expiry and renewal
- ✅ Access tokens available, refresh tokens hidden from app code

### Blazor Server (ElsaIdentity)

- ✅ JWT tokens stored server-side in session
- ✅ Automatic token refresh via `JwtAuthenticationProvider` (2 min skew)
- ✅ Single-flight coordination prevents concurrent refresh requests
- ✅ Tokens cleared on refresh failure

### Blazor WebAssembly (ElsaIdentity)

- ⚠️ JWT tokens stored in browser LocalStorage
- ✅ Automatic token refresh via `JwtAuthenticationProvider` (2 min skew)
- ✅ Single-flight coordination prevents concurrent refresh requests
- ✅ Tokens cleared on refresh failure

## File Structure

### Elsa.Studio.Authentication.Abstractions

```
Contracts/
├── IHttpConnectionOptionsConfigurator.cs   # SignalR auth configuration interface
├── ILoginMethodCatalog.cs                  # Contributes login-method metadata
├── ILoginMethodComponentProvider.cs        # Maps a method kind to a component
└── ILoginMethodIconProvider.cs             # Trusted local SVG icons

Models/
├── LoginMethodDescriptor.cs                # Safe presentation data + catalog result
├── LoginFailureCodes.cs                    # Fixed non-sensitive failure codes
├── StudioAuthenticationProvider.cs         # Provider enum
└── StudioAuthenticationProviderRegistration.cs

Options/
└── StudioAuthenticationOptions.cs          # Authentication:Provider

Validation/
└── StudioAuthenticationOptionsValidator.cs # Mutual-exclusion startup validation

ComponentProviders/
└── UnauthorizedComponentProvider.cs        # Generic unauthorized UI provider

Extensions/
└── ServiceCollectionExtensions.cs          # AddStudioAuthenticationMode()
```

### Elsa.Studio.Authentication.UI

```
Pages/
└── Login.razor                             # Owns /login

Components/
├── LoginThemeHost.razor                    # Resolves and renders the selected theme
├── LoginPanel.razor                        # Provider-neutral method composition
├── LoginMethodButton.razor
├── LoginUtilityLinks.razor
├── LoginThemeErrorBoundary.cs
├── LoginThemeRecovery.razor
└── Themes/                                 # classic-refined-split, classic-unified,
                                            # classic-brand-canvas

Contracts/
├── ILoginThemeProvider.cs
├── ILoginThemeRegistry.cs
└── LoginThemeComponentBase.cs

Options/
└── LoginThemeOptions.cs                    # Authentication:Login:Theme
```

### Elsa.Studio.ExternalAuthentication (shared)

```
Client/
├── IExternalAuthenticationBrokerApi.cs     # authorize / token / logout
├── IExternalAuthenticationConnectionsApi.cs
├── IExternalAuthenticationOperationsApi.cs
├── IExternalIdentityLinksApi.cs
└── IIdentityRolesApi.cs                    # (ILoginMethodsApi lives alongside the broker API)

Models/
├── BrokerModels.cs                         # Requests/responses, callback paths, client options
├── ConnectionModels.cs
├── IdentityLinkModels.cs
└── OperationModels.cs

Services/
├── ExternalAuthenticationLoginCoordinator.cs  # Host-neutral discovery base
├── ExternalAuthenticationLoginUi.cs           # Catalog, component and icon providers
├── BackendUriResolver.cs                      # Same-origin validation for broker URIs
├── ExternalAuthenticationPermissionService.cs
└── …                                          # UI state services

Components/                                    # Login methods, connection editor,
Pages/                                         # identity links, sessions, previews
Menu/
```

### Elsa.Studio.ExternalAuthentication.BlazorServer

```
Controllers/
└── ExternalAuthenticationController.cs         # login / callback / local-login / logout

Services/
├── ServerExternalAuthenticationStateProvider.cs   # Auth state + token provider + refresh
├── ServerExternalAuthenticationTicketStore.cs     # Server-side ticket (holds refresh token)
├── ServerExternalAuthenticationTransactionStore.cs# Data-protected PKCE/state cookie
├── ServerExternalAuthenticationRefreshCoordinator.cs # Single-flight rotation
├── ServerExternalAuthenticationLoginCoordinator.cs
├── ServerExternalAuthenticationAntiforgeryTokenProvider.cs
└── ExternalAuthenticationServerHttpConnectionOptionsConfigurator.cs

HttpMessageHandlers/
└── ExternalAuthenticationAuthenticatingApiHttpMessageHandler.cs

Components/
├── ChallengeToExternalLogin.razor              # Unauthorized redirect
└── BrokerLoginState.razor                      # App bar sign-out entry point
```

### Elsa.Studio.ExternalAuthentication.BlazorWasm

```
Pages/
└── ExternalAuthenticationCallback.razor        # Consumes the broker callback

Services/
├── ExternalAuthenticationWasmTokenProvider.cs  # Token access + rotation
├── ExternalAuthenticationWasmCallbackService.cs# Exact-origin callback + code exchange
├── ExternalAuthenticationWasmLoginCoordinator.cs
├── ExternalAuthenticationWasmLogoutService.cs
├── ExternalAuthenticationWasmAuthenticationStateProvider.cs
├── BrowserExternalAuthenticationPkceService.cs # Web Crypto S256 PKCE
├── BrowserExternalAuthenticationPkceTransactionStore.cs
├── BrowserExternalAuthenticationTokenStore.cs  # Memory / Session / Durable
└── ExternalAuthenticationHttpConnectionOptionsConfigurator.cs

Models/
├── ExternalAuthenticationBrokerOptions.cs      # ClientId, callbacks, BrowserStorage
├── ExternalAuthenticationPkceTransaction.cs
└── ExternalAuthenticationTokenSet.cs

Components/
├── NavigateToExternalLogin.razor
└── BrokerLoginState.razor
```

### Elsa.Studio.Authentication.OpenIdConnect.BlazorServer

```
Services/
├── TokenRefreshService.cs      # Core OAuth2 refresh token logic
├── AuthCookieEvents.cs         # OnValidatePrincipal for auto-refresh
├── ServerTokenProvider.cs      # ITokenProvider implementation
└── DirectOpenIdConnectLoginUi.cs # Login-method catalog + component provider

Models/
└── TokenRefreshResult.cs       # Refresh operation result

Components/
├── ChallengeToLogin.razor            # Unauthorized redirect component
└── DirectOpenIdConnectLoginMethod.razor

Controllers/
└── AuthenticationController.cs # Login/Logout endpoints

Extensions/
└── ServiceCollectionExtensions.cs  # AddOpenIdConnectAuth()
```

### Elsa.Studio.Authentication.OpenIdConnect (Shared)

```
Models/
└── OidcOptions.cs                          # Configuration options

Services/
└── OidcHttpConnectionOptionsConfigurator.cs  # SignalR connection auth config

HttpMessageHandlers/
└── OidcAuthenticatingApiHttpMessageHandler.cs

Contracts/
├── ITokenProvider.cs                       # Token provider interface
└── IBackendApiScopeProvider.cs             # Multi-audience scope resolution
```

### Elsa.Studio.Authentication.ElsaIdentity

```
Services/
├── JwtAuthenticationProvider.cs            # IAuthenticationProvider with auto-refresh
├── ElsaIdentityRefreshTokenService.cs      # Calls Elsa backend refresh endpoint
├── ElsaIdentityHttpConnectionOptionsConfigurator.cs  # SignalR connection auth config
├── JwtAccessorBase.cs                      # Base class for token storage
└── AccessTokenAuthenticationStateProvider.cs  # Blazor auth state

Contracts/
├── IAuthenticationProvider.cs              # Token provider interface
├── IRefreshTokenService.cs                 # Token refresh interface
├── IJwtAccessor.cs                         # Token storage interface
└── IJwtParser.cs                           # JWT parsing interface
```

## Troubleshooting

### Common Issues

1. **Startup fails with "Conflicting Elsa Studio authentication modes are registered"**
   - Two provider modules are registered at once. Register exactly one.
   - Common cause: leaving `AddLoginModule()` in place while also registering the shared authentication UI or a broker package.

2. **Startup fails with "Authentication:Provider selects 'X', but 'Y' handlers are also registered"**
   - The configuration value and the code path disagree. Check the `if/else` chain in `Program.cs` against the configured value.

3. **Token refresh not working (direct OIDC)**
   - Ensure `offline_access` scope is requested
   - Verify IdP issues refresh tokens
   - Check `SaveTokens = true` (default)

4. **401 errors on API calls**
   - Check token scopes match API requirements
   - For multi-audience direct OIDC: configure `BackendApiScopes`
   - For the broker: Elsa issues the credential used for its own API, so `BackendApiScopes` does not apply

5. **Refresh failures cause logout**
   - This is expected behavior — it ensures security
   - For the broker, a failed rotation is treated as possible refresh-token reuse or revocation, and local credentials are cleared

6. **Azure AD / Entra ID issues (direct OIDC)**
   - Use tenant-specific authority: `https://login.microsoftonline.com/{tenant}/v2.0`
   - Request `offline_access` for refresh tokens
   - For backend API: use `api://{app-id}/.default` scope

### External Authentication

7. **Startup fails on callback paths**
   - The callback paths are fixed to `/authentication/external/callback` and `/authentication/external/logout-callback`. Register the absolute Studio URLs with the matching Elsa Authentication Client instead of customizing the paths.

8. **"Studio WebAssembly is a public client and must not contain a broker client secret"**
   - Remove `ClientSecret` from the WASM host's `Authentication:ExternalAuthentication` section. Only the Server host is confidential.

9. **"The authentication callback URI does not match this Studio client's exact registered callback URI"**
   - Scheme, host, port, and path must match exactly. Check reverse-proxy rewriting, an `http`/`https` mismatch, or a non-default port.

10. **"The broker returned an initiation URI outside the configured Elsa backend origin"**
    - The connection's initiation URI is not same-origin with the configured `Backend` URL. Verify Elsa's public-origin configuration.

11. **WASM users are signed out on refresh or in a new tab**
    - Expected with the default `BrowserStorage: "Memory"`. `Session` or `Durable` changes this at a documented security cost.

12. **No sign-in methods appear on `/login`**
    - Discovery is anonymous against `GET /external-authentication/login-methods`. Confirm the backend is reachable, the `ClientId` matches an Elsa Authentication Client, and at least one connection is enabled.

## Resources

- [Migrate Studio from Direct OpenID Connect to Brokered Authentication](../docs/migrations/external-authentication.md)
- [Elsa.Studio.ExternalAuthentication README](../src/modules/Elsa.Studio.ExternalAuthentication/README.md)
- [Elsa.Studio.ExternalAuthentication.BlazorServer README](../src/modules/Elsa.Studio.ExternalAuthentication.BlazorServer/README.md)
- [Elsa.Studio.ExternalAuthentication.BlazorWasm README](../src/modules/Elsa.Studio.ExternalAuthentication.BlazorWasm/README.md)
- [Elsa.Studio.Authentication.UI README](../src/modules/Elsa.Studio.Authentication.UI/README.md)
- [Elsa.Studio.Authentication.Abstractions README](../src/modules/Elsa.Studio.Authentication.Abstractions/README.md)
- [Elsa.Studio.Authentication.OpenIdConnect README](../src/modules/Elsa.Studio.Authentication.OpenIdConnect/README.md)
- [Microsoft Blazor Authentication Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/)
