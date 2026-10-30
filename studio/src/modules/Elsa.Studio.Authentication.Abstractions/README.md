# Elsa Studio Authentication Abstractions

Shared abstractions for authentication providers in Elsa Studio.

## Overview

This package provides common interfaces that can be shared across different authentication provider implementations. In addition to SignalR and unauthorized-component contracts, it defines the provider-neutral login UI seam:

1. **ILoginMethodCatalog** - Lists safe login-method presentation metadata.
2. **ILoginMethodComponentProvider** - Maps a method kind to a typed Blazor component.
3. **ILoginMethodIconProvider** - Contributes trusted local SVG icons.
4. **IHttpConnectionOptionsConfigurator** - Configures SignalR authentication.
5. **UnauthorizedComponentProvider<TComponent>** - Renders provider-specific unauthorized UI.

Catalogs can identify a preferred method, but preference is visual metadata only. The shared
authentication shell always waits for an explicit user action. Provider modules reference this
abstractions package; the application composition root references and registers
`Elsa.Studio.Authentication.UI`.

## Purpose

The abstractions package allows:

1. **SignalR Authentication**: Provide a consistent way for authentication providers to configure SignalR connections
2. **Unauthorized UI Rendering**: Simplify registration of custom unauthorized components
3. **Extensibility**: Make it easy to add new authentication providers
4. **Decoupling**: Keep provider-specific code separate while maintaining shared contracts

## Abstractions

### IHttpConnectionOptionsConfigurator

Configures HTTP connection options for SignalR connections based on the active authentication provider.

```csharp
public interface IHttpConnectionOptionsConfigurator
{
    /// <summary>
    /// Configures the HTTP connection options for a SignalR connection.
    /// </summary>
    /// <param name="options">The HTTP connection options to configure.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ConfigureAsync(HttpConnectionOptions options, CancellationToken cancellationToken = default);
}
```

**Purpose**: Different authentication providers need to configure SignalR connections differently:
- OIDC providers set access token providers with specific scopes
- JWT-based providers set authorization headers
- Cookie-based providers configure cookie handling

**Usage**: Each authentication provider implements this interface to configure SignalR connections appropriately. The configurator is injected into services that need to establish SignalR connections (e.g., `WorkflowInstanceObserverFactory`).

### UnauthorizedComponentProvider<TComponent>

A generic component provider that renders a specific component type when users are unauthorized.

```csharp
public class UnauthorizedComponentProvider<TComponent> : IUnauthorizedComponentProvider 
    where TComponent : IComponent
{
    public RenderFragment GetUnauthorizedComponent() => builder => builder.CreateComponent<TComponent>();
}
```

**Purpose**: Eliminates the need to create separate provider classes for each unauthorized component.

**Usage**: Authentication modules register this generic provider with their specific unauthorized component type (e.g., login page, redirect component, challenge component).

## Example: Implementing a New Authentication Provider

### 1. Implement IHttpConnectionOptionsConfigurator

```csharp
using Elsa.Studio.Authentication.Abstractions.Contracts;
using Microsoft.AspNetCore.Http.Connections.Client;

public class CustomAuthHttpConnectionOptionsConfigurator : IHttpConnectionOptionsConfigurator
{
    private readonly ICustomAuthenticationProvider _authenticationProvider;

    public CustomAuthHttpConnectionOptionsConfigurator(ICustomAuthenticationProvider authenticationProvider)
    {
        _authenticationProvider = authenticationProvider;
    }

    public Task ConfigureAsync(HttpConnectionOptions connectionOptions, CancellationToken cancellationToken = default)
    {
        // Configure access token provider for SignalR connection
        connectionOptions.AccessTokenProvider = async () =>
        {
            var token = await _authenticationProvider.GetAccessTokenAsync(cancellationToken);
            return token ?? string.Empty;
        };

        return Task.CompletedTask;
    }
}
```

### 2. Create an Unauthorized Component

```csharp
using Microsoft.AspNetCore.Components;

public class CustomUnauthorizedPage : ComponentBase
{
    protected override void OnInitialized()
    {
        // Redirect to login, show message, etc.
    }
}
```

### 3. Register Services

```csharp
using Elsa.Studio.Authentication.Abstractions.ComponentProviders;
using Elsa.Studio.Contracts;

services.AddScoped<IHttpConnectionOptionsConfigurator, CustomAuthHttpConnectionOptionsConfigurator>();
services.AddScoped<IUnauthorizedComponentProvider, UnauthorizedComponentProvider<CustomUnauthorizedPage>>();
```

## Existing Implementations

The following authentication providers use these abstractions:

### OpenID Connect (OIDC)
- **IHttpConnectionOptionsConfigurator**: `OidcHttpConnectionOptionsConfigurator` - Configures SignalR with OIDC access tokens
- **IUnauthorizedComponentProvider**: 
  - `UnauthorizedComponentProvider<ChallengeToLogin>` (Blazor Server)
  - `UnauthorizedComponentProvider<NavigateToLogin>` (Blazor WebAssembly)

### Elsa Identity
- **IHttpConnectionOptionsConfigurator**: `ElsaIdentityHttpConnectionOptionsConfigurator` - Configures SignalR with JWT access tokens
- **IUnauthorizedComponentProvider**: `UnauthorizedComponentProvider<Unauthorized>` or `UnauthorizedComponentProvider<RedirectToLogin>`

### Login Module (deprecated)
- **IHttpConnectionOptionsConfigurator**: `LoginAuthHttpConnectionOptionsConfigurator` - Uses authentication provider manager
- **IUnauthorizedComponentProvider**: `UnauthorizedComponentProvider<RedirectToLogin>`

## How SignalR Configuration Works

1. **Service Registration**: Authentication modules register their `IHttpConnectionOptionsConfigurator` implementation
2. **SignalR Connection**: When a SignalR connection is needed (e.g., for real-time workflow updates), services inject the configurator
3. **Configuration**: The configurator sets up the connection options with appropriate authentication tokens
4. **Connection**: SignalR establishes the connection with the configured authentication

Example from `WorkflowInstanceObserverFactory`:

```csharp
public class WorkflowInstanceObserverFactory(
    IHttpConnectionOptionsConfigurator httpConnectionOptionsConfigurator) : IWorkflowInstanceObserverFactory
{
    public async Task<IWorkflowInstanceObserver> CreateAsync(WorkflowInstanceObserverContext context)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, async options =>
            {
                await httpConnectionOptionsConfigurator.ConfigureAsync(options, cancellationToken);
            })
            .Build();
        // ...
    }
}
```

## How Unauthorized Component Rendering Works

1. **Service Registration**: Authentication modules register their unauthorized component using `UnauthorizedComponentProvider<TComponent>`
2. **Authorization Check**: The app layout (e.g., `MainLayout.razor`) checks if the user is authorized
3. **Component Rendering**: If unauthorized, the layout retrieves the component from `IUnauthorizedComponentProvider.GetUnauthorizedComponent()`
4. **Display**: The unauthorized component is rendered (typically showing a login page or redirecting)

## Integration with Elsa Studio Core

These abstractions work alongside the authentication infrastructure defined in `Elsa.Studio.Core`:

```
Elsa.Studio.Core
├── IUnauthorizedComponentProvider  ← Interface for unauthorized UI
│
Elsa.Studio.Authentication.Abstractions
├── IHttpConnectionOptionsConfigurator     ← SignalR connection configuration
└── UnauthorizedComponentProvider<T>       ← Generic unauthorized component implementation
```

## Dependencies

This package references:
- **Elsa.Studio.Core** - Core abstractions and contracts
- **Microsoft.AspNetCore.SignalR.Client** - SignalR connection types

## Design Philosophy

### Why These Abstractions?

1. **SignalR Configuration**: All authentication providers need to configure SignalR connections, so a shared abstraction prevents code duplication
2. **Generic Component Provider**: Using generics eliminates boilerplate code for each unauthorized component
3. **Minimal Surface Area**: We only abstract what's truly common across providers
4. **Focused Purpose**: Each abstraction has a single, clear responsibility

### Why Not More Abstractions?

We intentionally keep the abstraction layer minimal:

- Different authentication providers have significantly different flows
- Over-abstraction can make implementations harder to understand
- Provider-specific optimizations should not be constrained by abstractions
- Token acquisition patterns vary too much to abstract effectively

## License

This package is part of Elsa Studio and follows the same license terms.
