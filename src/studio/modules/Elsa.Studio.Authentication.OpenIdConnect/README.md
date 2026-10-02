# Elsa Studio Authentication - OpenID Connect

> For new deployments that need administrators to manage multiple identity providers at runtime,
> prefer the External Authentication broker and its Identity provider connections UI. This direct OIDC module
> remains supported for a single deployment-configured provider and is not marked obsolete.

This module provides the shared OpenID Connect (OIDC) services used by Elsa Studio.
It is not used directly by host applications. Use one of the hosting-specific packages instead:

- `Elsa.Studio.Authentication.OpenIdConnect.BlazorServer`
- `Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm`

The implementation is based on Microsoft's ASP.NET Core and Blazor authentication stacks. Studio signs users in with the OIDC provider, obtains access tokens, and attaches those tokens to HTTP and SignalR calls to the Elsa backend API.

## Hosting Models

### Blazor Server

Use `Elsa.Studio.Authentication.OpenIdConnect.BlazorServer` when Elsa Studio runs as a Blazor Server app.

```csharp
using Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Extensions;

builder.Services.AddOpenIdConnectAuth(options =>
{
    options.Authority = "https://your-identity-server.com";
    options.ClientId = "elsa-studio";
    options.ClientSecret = "your-client-secret"; // Optional. Use only for confidential clients.
    options.AuthenticationScopes = ["openid", "profile", "offline_access"];
    options.BackendApiScopes = ["elsa_api"];
    options.UsePkce = true;
    options.SaveTokens = true;
});

// After UseRouting and before endpoint mapping.
app.UseAuthentication();
app.UseAuthorization();
```

Blazor Server uses ASP.NET Core cookie authentication plus the OpenID Connect handler. Tokens are stored in the encrypted authentication ticket and are not exposed to browser JavaScript.

### Blazor WebAssembly

Use `Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm` when Elsa Studio runs as a WebAssembly app.

```csharp
using Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm.Extensions;

builder.Services.AddOpenIdConnectAuth(options =>
{
    options.Authority = "https://your-identity-server.com";
    options.ClientId = "elsa-studio-wasm";
    options.AuthenticationScopes = ["openid", "profile", "offline_access"];
    options.BackendApiScopes = ["elsa_api"];
    options.ResponseType = "code";
});
```

Do not configure a client secret for WebAssembly or other browser-hosted clients. These are public clients and should use authorization code flow with PKCE.

The WebAssembly package provides the `/authentication/{action}` routes used by `RemoteAuthenticatorView`, so host projects do not need to add their own `Authentication.razor` page when using the standard Elsa Studio shell.

If your host uses the standard Elsa Studio shell `App` component, no Razor file changes are required. If you use a custom WebAssembly host, make sure `wwwroot/index.html` includes Microsoft's authentication script before `_framework/blazor.webassembly.js`:

```html
<script src="_content/Microsoft.AspNetCore.Components.WebAssembly.Authentication/AuthenticationService.js"></script>
<script src="_framework/blazor.webassembly.js"></script>
```

Without this script, the browser will report `AuthenticationService.init` as undefined during startup.

## Default Studio Hosts

The default Elsa Studio hosts select the authentication provider from configuration:

```json
{
  "Authentication": {
    "Provider": "OpenIdConnect",
    "OpenIdConnect": {
      "Authority": "https://your-identity-server.com",
      "ClientId": "elsa-studio",
      "ClientSecret": "",
      "AuthenticationScopes": ["openid", "profile", "offline_access"],
      "BackendApiScopes": ["elsa_api"],
      "SaveTokens": true
    }
  },
  "Backend": {
    "Url": "https://localhost:5001/elsa/api"
  }
}
```

`ClientSecret` is optional. Configure it only for confidential clients, such as a Blazor Server Studio host that can keep the secret server-side.

## Configuration Options

| Property | Description | Default |
| --- | --- | --- |
| `Authority` | OIDC provider authority URL. | Required |
| `ClientId` | Client ID registered with the provider. | Required |
| `ClientSecret` | Optional client secret for confidential clients. Do not use from WebAssembly. | `null` |
| `AuthenticationScopes` | Scopes requested during sign-in. | `["openid", "profile", "offline_access"]` |
| `BackendApiScopes` | Scopes requested for tokens sent to the Elsa backend API. | `[]` |
| `ResponseType` | OAuth/OIDC response type. | `"code"` |
| `UsePkce` | Enables PKCE in the Blazor Server OIDC handler. | `true` |
| `SaveTokens` | Saves tokens in server auth properties. Blazor Server only. | `true` |
| `CallbackPath` | Sign-in callback path. Server default: `/signin-oidc`; WASM default: `/authentication/login-callback`. | Host-specific |
| `SignedOutCallbackPath` | Sign-out callback path. Server default: `/signout-callback-oidc`; WASM default: `/authentication/logout-callback`. | Host-specific |
| `RequireHttpsMetadata` | Requires HTTPS metadata endpoints. | `true` |
| `GetClaimsFromUserInfoEndpoint` | Calls the OIDC UserInfo endpoint after sign-in. | `false` |
| `MetadataAddress` | Optional metadata endpoint override. | Auto-discovered |
| `NameClaimType` | Claim type used for the user name. | `"name"` |
| `RoleClaimType` | Claim type used for roles. | `"role"` |

## Authentication Scopes vs Backend API Scopes

Use `AuthenticationScopes` for the sign-in flow. These are usually identity scopes such as `openid`, `profile`, `email`, and optionally `offline_access`.

Use `BackendApiScopes` for access tokens sent to the Elsa backend API. This is useful when the backend API has its own audience or scope.

```json
{
  "Authentication": {
    "OpenIdConnect": {
      "AuthenticationScopes": ["openid", "profile", "offline_access"],
      "BackendApiScopes": ["api://your-api-app-id/elsa-server-api"]
    }
  }
}
```

The `OidcAuthenticatingApiHttpMessageHandler` attaches a bearer token to outgoing Elsa backend API requests. SignalR connections are configured by `OidcHttpConnectionOptionsConfigurator`.

## Client Secrets

A client secret authenticates the OAuth client application to the identity provider. It is not the user's password, and Elsa Studio does not send the user's credentials to the identity provider.

Use a client secret only when the Studio host is a confidential client that can protect it, such as Blazor Server. For WebAssembly, SPA, mobile, and other public clients, leave `ClientSecret` unset and rely on authorization code flow with PKCE.

## Token Refresh

### Blazor Server

Blazor Server uses ASP.NET Core Cookie plus OpenID Connect authentication. When `SaveTokens = true`, the access token, refresh token if issued, and expiration metadata are stored in the authentication ticket.

Access tokens are refreshed by `AuthCookieEvents.OnValidatePrincipal`. On each HTTP request, the cookie handler checks whether the access token is close to expiry and, if needed, calls the provider token endpoint with the refresh token grant.

Refresh prerequisites:

- `SaveTokens = true`
- `offline_access` requested when the provider requires it for refresh tokens
- the identity provider issues refresh tokens to this client

### Blazor WebAssembly

WebAssembly uses `Microsoft.AspNetCore.Components.WebAssembly.Authentication`. Token acquisition and refresh are handled by the framework through `IAccessTokenProvider`.

## Microsoft Entra ID Notes

For Microsoft Entra ID, prefer a tenant-specific authority:

```text
https://login.microsoftonline.com/{tenant-id}/v2.0
```

Entra ID v2.0 allows scopes for one resource per token request. Do not mix Microsoft Graph scopes with custom API scopes in the same backend API token request. If Studio needs a token for the Elsa backend API, place that API scope in `BackendApiScopes`.

If the UserInfo endpoint returns 401, leave `GetClaimsFromUserInfoEndpoint` disabled unless your app registration and scopes explicitly allow calling UserInfo.

## Migration from `Elsa.Studio.Login`

Legacy OIDC through `Elsa.Studio.Login` configured explicit authorization, token, and end-session endpoints:

```csharp
builder.Services.AddLoginModule();
builder.Services.UseOpenIdConnect(options =>
{
    options.AuthEndpoint = "https://identity-server.com/connect/authorize";
    options.TokenEndpoint = "https://identity-server.com/connect/token";
    options.EndSessionEndpoint = "https://identity-server.com/connect/endsession";
    options.ClientId = "elsa-studio";
    options.ClientSecret = "secret";
    options.Scopes = ["openid", "profile", "elsa_api"];
});
```

The current OIDC modules use authority metadata discovery and hosting-specific framework integration:

```csharp
builder.Services.AddOpenIdConnectAuth(options =>
{
    options.Authority = "https://identity-server.com";
    options.ClientId = "elsa-studio";
    options.ClientSecret = "secret"; // Blazor Server confidential clients only.
    options.AuthenticationScopes = ["openid", "profile", "offline_access"];
    options.BackendApiScopes = ["elsa_api"];
});
```

## Related Modules

- `Elsa.Studio.Authentication.OpenIdConnect.BlazorServer`
- `Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm`
- `Elsa.Studio.Authentication.Abstractions`
