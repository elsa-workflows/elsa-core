using Elsa.Studio.Authentication.Abstractions.Contracts;
using Microsoft.AspNetCore.Http.Connections.Client;

namespace Elsa.Studio.Host.CustomElements.Services;

/// <summary>
/// An <see cref="IHttpConnectionOptionsConfigurator"/> implementation that configures SignalR connections
/// using credentials supplied via the <see cref="BackendService"/> (API key or bearer access token).
/// </summary>
/// <remarks>
/// The Custom Elements host does not use the Login module; authentication credentials are provided
/// by the host application (e.g. via the <c>access-token</c> attribute on the custom element).
/// This configurator bridges those credentials to SignalR connections so that components such as
/// <c>WorkflowInstanceObserverFactory</c> can authenticate against secured backends.
/// </remarks>
public class BackendServiceHttpConnectionOptionsConfigurator(BackendService backendService) : IHttpConnectionOptionsConfigurator
{
    /// <inheritdoc />
    public Task ConfigureAsync(HttpConnectionOptions options, CancellationToken cancellationToken = default)
    {
        var apiKey = backendService.ApiKey;
        var accessToken = backendService.AccessToken;

        if (!string.IsNullOrEmpty(apiKey))
        {
            options.Headers["Authorization"] = $"ApiKey {apiKey}";
        }
        else
        {
            options.AccessTokenProvider = () => Task.FromResult<string?>(backendService.AccessToken);
        }

        return Task.CompletedTask;
    }
}
