using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.OrchardCore.Client.Extensions;

[PublicAPI]
public static class OrchardCoreClientServiceCollectionExtensions
{
    public static IServiceCollection AddOrchardCoreClient(this IServiceCollection services)
    {
        services.AddTransient<ISecurityTokenService, DefaultSecurityTokenService>();
        services.AddTransient<AuthenticatingDelegatingHandler>();
        
        services.AddHttpClient<ISecurityTokenClient, DefaultSecurityTokenClient>((sp, httpClient) =>
        {
            var options = sp.GetRequiredService<IOptions<OrchardCoreClientOptions>>().Value;
            httpClient.BaseAddress = options.BaseAddress;
        });
        
        services.AddHttpClient<IGraphQLClient, DefaultGraphQlClient>((sp, httpClient) =>
        {
            var options = sp.GetRequiredService<IOptions<OrchardCoreClientOptions>>().Value;
            httpClient.BaseAddress = options.BaseAddress;
        }).AddHttpMessageHandler<AuthenticatingDelegatingHandler>();
        
        return services;
    }
}