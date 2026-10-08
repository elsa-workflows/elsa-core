using Elsa.Studio.Authentication.Abstractions.Contracts;
using Elsa.Studio.Login.Contracts;
using Microsoft.AspNetCore.Http.Connections.Client;

namespace Elsa.Studio.Login.Services;

/// <summary>
/// Login authentication implementation of <see cref="IHttpConnectionOptionsConfigurator"/> that configures SignalR connections
/// to use JWT access tokens.
/// </summary>
public class LoginAuthHttpConnectionOptionsConfigurator(IAuthenticationProviderManager authenticationProvider) : IHttpConnectionOptionsConfigurator
{
    /// <inheritdoc />
    public Task ConfigureAsync(HttpConnectionOptions connectionOptions, CancellationToken cancellationToken = default)
    {
        // Configure access token provider for SignalR connection
        connectionOptions.AccessTokenProvider = async () =>
        {
            var token = await authenticationProvider.GetAuthenticationTokenAsync(TokenNames.AccessToken, cancellationToken);
            return token ?? string.Empty;
        };

        return Task.CompletedTask;
    }
}
