using System.Text.Json;
using Elsa.Api.Client.Contracts;
using Elsa.Api.Client.Options;
using Elsa.Api.Client.Resources.ActivityDescriptors.Contracts;
using Elsa.Api.Client.Resources.Identity.Contracts;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;
using Elsa.Api.Client.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Refit;

namespace Elsa.Api.Client.Extensions;

/// <summary>
/// Provides extension methods for dependency injection.
/// </summary>
[PublicAPI]
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Adds the Elsa client to the service collection.
    /// </summary>
    public static IServiceCollection AddElsaClient(this IServiceCollection services, Action<ElsaClientOptions> configureOptions)
    {
        services.Configure(configureOptions);
        services.AddSingleton<IElsaClient, ElsaClient>();

        JsonSerializerOptions jsonSerializerSettings = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            
        };
        
        var settings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(jsonSerializerSettings)
        };
        services.AddElsaApiClient<IWorkflowDefinitionsApi>(settings);
        services.AddElsaApiClient<IActivityDescriptorsApi>(settings);
        services.AddElsaApiClient<ILoginApi>(settings);
        return services;
    }

    /// <summary>
    /// Adds a refit client for the specified API type.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="settings">The refit settings.</param>
    /// <typeparam name="T">The API type.</typeparam>
    public static void AddElsaApiClient<T>(this IServiceCollection services, RefitSettings settings) where T : class
    {
        services.AddRefitClient<T>(settings)
            .ConfigureHttpClient(ConfigureElsaApiHttpClient);
    }

    private static void ConfigureElsaApiHttpClient(IServiceProvider serviceProvider, HttpClient httpClient)
    {
        var options = serviceProvider.GetRequiredService<IOptions<ElsaClientOptions>>().Value;
        httpClient.BaseAddress = options.BaseAddress;
    }
}