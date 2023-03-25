using Elsa.Api.Client.Contracts;
using Elsa.Api.Client.Options;
using Elsa.Api.Client.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Refit;

namespace Elsa.Api.Client.Extensions;

/// <summary>
/// Provides extension methods for dependency injection.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Adds the Elsa client to the service collection.
    /// </summary>
    public static IServiceCollection AddElsaClient(this IServiceCollection services, Action<ElsaClientOptions> configureOptions)
    {
        services.Configure(configureOptions);
        services.AddSingleton<IElsaClient, ElsaClient>();
        
        var settings = new RefitSettings();
        services.AddClient<IWorkflowDefinitionsApi>(settings);
        return services;
    }

    private static void AddClient<T>(this IServiceCollection services, RefitSettings settings) where T : class
    {
        services.AddRefitClient<T>(settings).ConfigureHttpClient(ConfigureHttpClient);
    }

    private static void ConfigureHttpClient(IServiceProvider serviceProvider, HttpClient httpClient)
    {
        var options = serviceProvider.GetRequiredService<IOptions<ElsaClientOptions>>().Value;
        httpClient.BaseAddress = options.BaseAddress;
    }
}