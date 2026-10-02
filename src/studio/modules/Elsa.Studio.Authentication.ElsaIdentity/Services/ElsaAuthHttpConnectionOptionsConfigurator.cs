using Elsa.Studio.Authentication.Abstractions.Contracts;
using Elsa.Studio.Authentication.ElsaIdentity.Contracts;
using Microsoft.AspNetCore.Http.Connections.Client;

namespace Elsa.Studio.Authentication.ElsaIdentity.Services;

/// <summary>
/// ElsaIdentity implementation of <see cref="IHttpConnectionOptionsConfigurator"/> that configures SignalR connections
/// to use JWT access tokens.
/// </summary>
public class ElsaIdentityHttpConnectionOptionsConfigurator(ITokenProvider tokenProvider) : IHttpConnectionOptionsConfigurator
{
    /// <inheritdoc />
    public Task ConfigureAsync(HttpConnectionOptions connectionOptions, CancellationToken cancellationToken = default)
    {
        // Configure access token provider for SignalR connection.
        connectionOptions.AccessTokenProvider = async () => await tokenProvider.GetAccessTokenAsync(cancellationToken);

        return Task.CompletedTask;
    }
}
