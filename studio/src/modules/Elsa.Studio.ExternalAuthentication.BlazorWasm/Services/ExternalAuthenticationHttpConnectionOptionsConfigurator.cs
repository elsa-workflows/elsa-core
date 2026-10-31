using Elsa.Studio.Authentication.Abstractions.Contracts;
using Elsa.Studio.ExternalAuthentication.Models;
using Microsoft.AspNetCore.Http.Connections.Client;

namespace Elsa.Studio.ExternalAuthentication.BlazorWasm.Services;

/// <summary>Supplies broker access tokens to Studio SignalR connections.</summary>
public sealed class ExternalAuthenticationHttpConnectionOptionsConfigurator(
    IExternalAuthenticationTokenProvider tokenProvider) : IHttpConnectionOptionsConfigurator
{
    /// <inheritdoc />
    public Task ConfigureAsync(HttpConnectionOptions connectionOptions, CancellationToken cancellationToken = default)
    {
        connectionOptions.AccessTokenProvider = () => tokenProvider.GetAccessTokenAsync(cancellationToken);
        return Task.CompletedTask;
    }
}
