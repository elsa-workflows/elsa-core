using Elsa.Api.Client.Options;
using Elsa.Studio.AI.Client;
using Elsa.Studio.AI.Contracts;
using Elsa.Studio.AI.Menu;
using Elsa.Studio.AI.Services;
using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.AI.Extensions;

/// <summary>
/// Contains extension methods for the Weaver AI module.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Weaver AI module.
    /// </summary>
    public static IServiceCollection AddWeaverModule(this IServiceCollection services, BackendApiConfig backendApiConfig)
    {
        var clientOptions = new ElsaClientBuilderOptions();
        backendApiConfig.ConfigureHttpClientBuilder?.Invoke(clientOptions);
        var streamClientBuilder = services.AddHttpClient<WeaverStreamClient>((serviceProvider, client) =>
        {
            client.Timeout = Timeout.InfiniteTimeSpan;
            clientOptions.ConfigureHttpClient?.Invoke(serviceProvider, client);
        });

        if (clientOptions.AuthenticationHandler != null)
            streamClientBuilder.AddHttpMessageHandler(serviceProvider => (DelegatingHandler)serviceProvider.GetRequiredService(clientOptions.AuthenticationHandler));

        clientOptions.ConfigureHttpClientBuilder?.Invoke(streamClientBuilder);

        return services
            .AddScoped<IFeature, Feature>()
            .AddScoped<IMenuProvider, WeaverMenu>()
            .AddScoped<IWeaverService, RemoteWeaverService>()
            .AddRemoteApi<IWeaverApi>(backendApiConfig);
    }
}
