# Elsa.Studio.Authentication.ElsaIdentity

Core authentication infrastructure for Elsa Studio using JWT-based authentication with Elsa Identity (username/password authentication against the Elsa backend API).

## Overview

This module provides the shared authentication services and abstractions for implementing JWT-based authentication with Elsa Identity. It includes:

- JWT token parsing and validation
- Credentials validation against Elsa backend
- Token refresh services
- Authentication state management
- HTTP message handlers for authenticated API calls
- SignalR connection configuration

**Note**: This is a platform-agnostic core module. Use one of the hosting-specific modules to integrate with your application:
- `Elsa.Studio.Authentication.ElsaIdentity.BlazorServer` - For Blazor Server applications
- `Elsa.Studio.Authentication.ElsaIdentity.BlazorWasm` - For Blazor WebAssembly applications

## Key Components

### Contracts

- **`ITokenProvider`** - Provides access to authentication tokens and user information
- **`ICredentialsValidator`** - Validates username/password credentials against Elsa backend
- **`IJwtAccessor`** - Platform-specific JWT token storage abstraction (implemented by hosting modules)
- **`IJwtParser`** - Parses JWT tokens to extract claims and expiration
- **`IRefreshTokenService`** - Handles token refresh with the Elsa Identity API

### Services

- **`AccessTokenAuthenticationStateProvider`** - Blazor authentication state provider based on JWT access tokens
- **`ElsaIdentityCredentialsValidator`** - Validates credentials against the Elsa Identity `/identity/login` endpoint
- **`ElsaIdentityRefreshTokenService`** - Refreshes access tokens using refresh tokens via the `/identity/refresh` endpoint
- **`JwtAuthenticationProvider`** - Main authentication provider implementation
- **`JwtParser`** - JWT parsing and validation implementation
- **`ElsaIdentityHttpConnectionOptionsConfigurator`** - Configures SignalR connections with JWT authentication

### HTTP Message Handlers

- **`ElsaIdentityAuthenticatingApiHttpMessageHandler`** - Automatically adds JWT bearer tokens to HTTP requests to the Elsa backend API

## Usage

This core module is not used directly. Instead, use one of the hosting-specific modules:

### Blazor Server

```csharp
using Elsa.Studio.Authentication.ElsaIdentity.BlazorServer.Extensions;

builder.Services.AddElsaIdentity();
```

### Blazor WebAssembly

```csharp
using Elsa.Studio.Authentication.ElsaIdentity.BlazorWasm.Extensions;

builder.Services.AddElsaIdentity();
```

Both extensions call `AddElsaIdentityCore()` from this module to register the shared services, then add their platform-specific `IJwtAccessor` implementation.

## Architecture

### JWT Token Storage

Tokens are stored using `IJwtAccessor`, which has platform-specific implementations:

- **Blazor Server**: `BlazorServerJwtAccessor` - Stores tokens in browser local storage with prerendering support
- **Blazor WebAssembly**: `BlazorWasmJwtAccessor` - Stores tokens in browser local storage

Token names are defined in `TokenNames`:
- `AccessToken` - The JWT access token
- `RefreshToken` - The refresh token for silent token renewal

### Authentication Flow

1. User provides credentials via login UI
2. `ICredentialsValidator` validates credentials with Elsa backend
3. On success, tokens are stored via `IJwtAccessor`
4. `AccessTokenAuthenticationStateProvider` parses the JWT and provides authentication state
5. `ElsaIdentityAuthenticatingApiHttpMessageHandler` automatically adds tokens to API requests
6. `ElsaIdentityRefreshTokenService` refreshes tokens when they expire

## Dependencies

- **Blazored.LocalStorage** - Browser local storage access
- **Microsoft.AspNetCore.Components.Authorization** - Blazor authentication infrastructure
- **Microsoft.AspNetCore.SignalR.Client** - SignalR client for real-time connections

## Related Modules

- **`Elsa.Studio.Authentication.ElsaIdentity.BlazorServer`** - Blazor Server hosting implementation
- **`Elsa.Studio.Authentication.ElsaIdentity.BlazorWasm`** - Blazor WebAssembly hosting implementation
- **`Elsa.Studio.Authentication.ElsaIdentity.UI`** - Login UI components and pages
- **`Elsa.Studio.Authentication.OpenIdConnect`** - Alternative OpenID Connect authentication provider

## Configuration

### Identity Token Options

Configure Elsa Identity endpoints via `IdentityTokenOptions`:

```csharp
builder.Services.Configure<IdentityTokenOptions>(options =>
{
    options.LoginUrl = "https://your-elsa-api.com/identity/login";
    options.RefreshUrl = "https://your-elsa-api.com/identity/refresh";
});
```

By default, these endpoints are relative to the configured Elsa backend API URL.

## Token Refresh

The module automatically refreshes access tokens when they expire using the refresh token:

1. API requests detect expired tokens via `ElsaIdentityAuthenticatingApiHttpMessageHandler`
2. `IRefreshTokenService` calls the Elsa Identity refresh endpoint with the refresh token
3. New tokens are stored via `IJwtAccessor`
4. The original API request is retried with the new token

## Security

- Tokens are stored in browser local storage (both Server and WASM)
- JWT signatures are validated during parsing
- Expired tokens trigger automatic refresh attempts
- Failed refresh attempts clear tokens and trigger re-authentication
- All API communication uses HTTPS (enforced by configuration)

## Troubleshooting

### Tokens not persisting across sessions

- Ensure `Blazored.LocalStorage` is properly registered
- Check browser console for local storage errors
- Verify tokens are being written via `IJwtAccessor.WriteTokenAsync()`

### Authentication state not updating

- Ensure `AccessTokenAuthenticationStateProvider` is registered as `AuthenticationStateProvider`
- Call `NotifyAuthenticationStateChanged()` after token changes
- Check that JWT contains valid claims (name, email, etc.)

### Token refresh failing

- Verify Elsa backend `/identity/refresh` endpoint is accessible
- Check that refresh token is present in storage
- Ensure refresh token hasn't expired (check backend token lifetime settings)
- Review HTTP client logs for detailed error messages
