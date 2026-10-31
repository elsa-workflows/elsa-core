using Microsoft.AspNetCore.SignalR.Client;

namespace Elsa.Studio.Workflows.Extensions;

/// <summary>
/// Provides extension methods for hub connection.
/// </summary>
public static class HubConnectionExtensions
{
    /// <summary>
    /// Configures the url.
    /// </summary>
    /// <param name="builder">The builder to configure.</param>
    /// <param name="url">The url.</param>
    /// <param name="clientFactory">The client factory.</param>
    /// <returns>The result of the operation.</returns>
    public static IHubConnectionBuilder WithUrl(this IHubConnectionBuilder builder, string url, IHttpMessageHandlerFactory clientFactory)
    {
        return builder.WithUrl(url, options =>
        {
            options.HttpMessageHandlerFactory = _ => clientFactory.CreateHandler();
        });
    }
}