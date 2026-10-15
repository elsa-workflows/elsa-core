using Elsa.Studio.Authentication.Abstractions.Contracts;
using Elsa.Studio.ExternalAuthentication.Models;
using Microsoft.AspNetCore.Http.Connections.Client;

namespace Elsa.Studio.ExternalAuthentication.BlazorServer.Services;

/// <summary>Supplies the server-held broker access token to Studio SignalR connections.</summary>
public sealed class ExternalAuthenticationServerHttpConnectionOptionsConfigurator(
    IExternalAuthenticationTokenProvider tokenProvider) : IHttpConnectionOptionsConfigurator
{
    /// <inheritdoc />
    public Task ConfigureAsync(HttpConnectionOptions connectionOptions, CancellationToken cancellationToken = default)
    {
        connectionOptions.AccessTokenProvider = () => tokenProvider.GetAccessTokenAsync(cancellationToken);
        return Task.CompletedTask;
    }
}
