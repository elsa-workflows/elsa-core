using Microsoft.AspNetCore.Http.Connections.Client;

namespace Elsa.Studio.Authentication.Abstractions.Contracts;

/// <summary>
/// Configures HTTP connection options for SignalR connections based on the active authentication provider.
/// </summary>
/// <remarks>
/// Different authentication providers may configure connections differently:
/// - OIDC providers may set access token providers with specific scopes
/// - JWT-based providers may set authorization headers
/// - Cookie-based providers may configure cookie handling
/// This interface allows each provider to configure connections appropriately.
/// </remarks>
public interface IHttpConnectionOptionsConfigurator
{
    /// <summary>
    /// Configures the HTTP connection options for a SignalR connection.
    /// </summary>
    /// <param name="options">The HTTP connection options to configure.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ConfigureAsync(HttpConnectionOptions options, CancellationToken cancellationToken = default);
}
