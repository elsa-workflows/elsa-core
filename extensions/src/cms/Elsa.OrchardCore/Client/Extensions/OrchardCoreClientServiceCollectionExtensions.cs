using Elsa.OrchardCore.Client.Contracts;
using Elsa.OrchardCore.Client.DelegatingHandlers;
using Elsa.OrchardCore.Client.Options;
using Elsa.OrchardCore.Client.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.OrchardCore.Client.Extensions;

/// <summary>
/// Provides extension methods for registering Orchard Core client services into an <see cref="IServiceCollection"/>.
/// </summary>
[PublicAPI]
public static class OrchardCoreClientServiceCollectionExtensions
{
    /// <summary>
    /// Registers services required for interacting with an Orchard Core API.
    /// This includes token authentication, GraphQL, and REST API clients.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddOrchardCoreClient(this IServiceCollection services)
    {
        services.AddTransient<ISecurityTokenService, DefaultSecurityTokenService>();
        services.AddTransient<AuthenticatingDelegatingHandler>();
        
        services.AddHttpClient<ISecurityTokenClient, DefaultSecurityTokenClient>((sp, httpClient) =>
        {
            var options = sp.GetRequiredService<IOptions<OrchardCoreClientOptions>>().Value;
            httpClient.BaseAddress = options.BaseAddress;
        });
        
        services.AddHttpClient<IGraphQLClient, DefaultGraphQLClient>((sp, httpClient) =>
        {
            var options = sp.GetRequiredService<IOptions<OrchardCoreClientOptions>>().Value;
            httpClient.BaseAddress = options.BaseAddress;
        }).AddHttpMessageHandler<AuthenticatingDelegatingHandler>();
        
        services.AddHttpClient<IRestApiClient, DefaultRestApiClient>((sp, httpClient) =>
        {
            var options = sp.GetRequiredService<IOptions<OrchardCoreClientOptions>>().Value;
            httpClient.BaseAddress = options.BaseAddress;
        }).AddHttpMessageHandler<AuthenticatingDelegatingHandler>();
        
        return services;
    }
}