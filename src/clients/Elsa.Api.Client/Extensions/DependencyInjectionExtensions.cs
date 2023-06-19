using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Api.Client.ActivityProviders;
using Elsa.Api.Client.Contracts;
using Elsa.Api.Client.Converters;
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
        services.AddActivityTypeService();

        services.AddApi<IWorkflowDefinitionsApi>(CreateRefitSettings);
        services.AddApi<IActivityDescriptorsApi>(CreateRefitSettings);
        services.AddApi<ILoginApi>(CreateRefitSettings);
        return services;
    }

    /// <summary>
    /// Adds activity type services to the service collection.
    /// </summary>
    public static IServiceCollection AddActivityTypeService(this IServiceCollection services)
    {
        services.AddSingleton<IActivityTypeService, DefaultActivityTypeService>();
        services.AddActivityTypeResolver<DefaultActivityTypeResolver>();
        services.AddActivityTypeResolver<FlowchartActivityTypeResolver>();
        return services;
    }
    
    /// <summary>
    /// Adds a refit client for the specified API type.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="settings">The refit settings.</param>
    /// <typeparam name="T">The API type.</typeparam>
    public static void AddApi<T>(this IServiceCollection services, Func<IServiceProvider, RefitSettings> settings) where T : class
    {
        services.AddRefitClient<T>(settings)
            .ConfigureHttpClient(ConfigureElsaApiHttpClient);
    }
    
    public static IServiceCollection AddActivityTypeResolver<T>(this IServiceCollection services) where T : class, IActivityTypeResolver
    {
        return services.AddSingleton<IActivityTypeResolver, T>();
    }

    private static void ConfigureElsaApiHttpClient(IServiceProvider serviceProvider, HttpClient httpClient)
    {
        var options = serviceProvider.GetRequiredService<IOptions<ElsaClientOptions>>().Value;
        httpClient.BaseAddress = options.BaseAddress;
    }
    
    private static RefitSettings CreateRefitSettings(IServiceProvider serviceProvider)
    {
        JsonSerializerOptions jsonSerializerSettings = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
        
        jsonSerializerSettings.Converters.Add(new JsonStringEnumConverter());
        jsonSerializerSettings.Converters.Add(new VersionOptionsJsonConverter());
        jsonSerializerSettings.Converters.Add(new ActivityJsonConverterFactory(serviceProvider));
        
        var settings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(jsonSerializerSettings)
        };    
            
        return settings;
    }

}