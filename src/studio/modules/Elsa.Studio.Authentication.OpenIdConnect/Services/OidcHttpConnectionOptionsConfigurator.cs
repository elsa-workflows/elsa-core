using Elsa.Studio.Authentication.Abstractions.Contracts;
using Elsa.Studio.Authentication.OpenIdConnect.Contracts;
using Microsoft.AspNetCore.Http.Connections.Client;

namespace Elsa.Studio.Authentication.OpenIdConnect.Services;

/// <summary>
/// OIDC implementation of <see cref="IHttpConnectionOptionsConfigurator"/> that configures SignalR connections
/// to use OIDC access tokens with appropriate scopes.
/// </summary>
public class OidcHttpConnectionOptionsConfigurator(ITokenProvider tokenProvider) : IHttpConnectionOptionsConfigurator
{
    /// <inheritdoc />
    public Task ConfigureAsync(HttpConnectionOptions connectionOptions, CancellationToken cancellationToken = default)
    {
        connectionOptions.AccessTokenProvider = async () => await tokenProvider.GetAccessTokenAsync(cancellationToken);
        return Task.CompletedTask;
    }
}
