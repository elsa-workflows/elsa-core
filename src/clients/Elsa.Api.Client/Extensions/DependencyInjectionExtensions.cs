using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Api.Client.Contracts;
using Elsa.Api.Client.Converters;
using Elsa.Api.Client.HttpMessageHandlers;
using Elsa.Api.Client.Options;
using Elsa.Api.Client.Resources.ActivityDescriptors.Contracts;
using Elsa.Api.Client.Resources.ActivityExecutions.Contracts;
using Elsa.Api.Client.Resources.Features.Contracts;
using Elsa.Api.Client.Resources.Identity.Contracts;
using Elsa.Api.Client.Resources.IncidentStrategies.Contracts;
using Elsa.Api.Client.Resources.Scripting.Contracts;
using Elsa.Api.Client.Resources.StorageDrivers.Contracts;
using Elsa.Api.Client.Resources.VariableTypes.Contracts;
using Elsa.Api.Client.Resources.WorkflowActivationStrategies.Contracts;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;
using Elsa.Api.Client.Resources.WorkflowInstances.Contracts;
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
    public static IServiceCollection AddElsaClient(this IServiceCollection services, Action<ElsaClientOptions>? configureOptions = default)
    {
        return services.AddElsaClient(builder => builder.AddHttpMessageHandler<ApiHttpMessageHandler>() , configureOptions);
    }

    /// <summary>
    /// Adds the Elsa client to the service collection.
    /// </summary>
    public static IServiceCollection AddElsaClient(this IServiceCollection services, Action<IHttpClientBuilder> configureHttpClientBuilder, Action<ElsaClientOptions>? configureOptions = default)
    {
        services.Configure(configureOptions ?? (_ => { }));
        services.AddScoped<ApiHttpMessageHandler>();
        services.AddScoped<IElsaClient, ElsaClient>();

        services.AddApi<IWorkflowDefinitionsApi>(CreateRefitSettings, configureHttpClientBuilder);
        services.AddApi<IWorkflowInstancesApi>(CreateRefitSettings, configureHttpClientBuilder);
        services.AddApi<IActivityDescriptorsApi>(CreateRefitSettings, configureHttpClientBuilder);
        services.AddApi<IActivityExecutionsApi>(CreateRefitSettings, configureHttpClientBuilder);
        services.AddApi<IStorageDriversApi>(CreateRefitSettings, configureHttpClientBuilder);
        services.AddApi<IVariableTypesApi>(CreateRefitSettings, configureHttpClientBuilder);
        services.AddApi<IWorkflowActivationStrategiesApi>(CreateRefitSettings, configureHttpClientBuilder);
        services.AddApi<IIncidentStrategiesApi>(CreateRefitSettings, configureHttpClientBuilder);
        services.AddApi<ILoginApi>(CreateRefitSettings, configureHttpClientBuilder);
        services.AddApi<IFeaturesApi>(CreateRefitSettings, configureHttpClientBuilder);
        services.AddApi<IJavaScriptApi>(CreateRefitSettings, configureHttpClientBuilder);
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
        services.AddApi<T>(settings, builder => builder.AddHttpMessageHandler<ApiHttpMessageHandler>());
    }
    
    /// <summary>
    /// Adds a refit client for the specified API type.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="settings">The refit settings.</param>
    /// <param name="configureHttpClientBuilder">A delegate to configure the HTTP client builder.</param>
    /// <typeparam name="T">The API type.</typeparam>
    public static void AddApi<T>(this IServiceCollection services, Func<IServiceProvider, RefitSettings> settings, Action<IHttpClientBuilder> configureHttpClientBuilder) where T : class
    {
        var builder = services.AddRefitClient<T>(settings).ConfigureHttpClient(ConfigureElsaApiHttpClient);
        configureHttpClientBuilder(builder);
    }
    
    /// <summary>
    /// Creates an API client for the specified API type.
    /// </summary>
    public static T CreateApi<T>(this IServiceProvider serviceProvider, Uri baseAddress) where T : class
    {
        return RestService.For<T>(baseAddress.ToString(), CreateRefitSettings(serviceProvider));
    }
    
    /// <summary>
    /// Creates an API client for the specified API type.
    /// </summary>
    public static T CreateApi<T>(this IServiceProvider serviceProvider, HttpClient httpClient) where T : class
    {
        return RestService.For<T>(httpClient, CreateRefitSettings(serviceProvider));
    }

    private static void ConfigureElsaApiHttpClient(IServiceProvider serviceProvider, HttpClient httpClient)
    {
        var options = serviceProvider.GetRequiredService<IOptions<ElsaClientOptions>>().Value;
        httpClient.BaseAddress = options.BaseAddress;
        options.ConfigureHttpClient?.Invoke(serviceProvider, httpClient);
    }

    /// <summary>
    /// Creates a <see cref="RefitSettings"/> instance configured for Elsa. 
    /// </summary>
    private static RefitSettings CreateRefitSettings(IServiceProvider serviceProvider)
    {
        JsonSerializerOptions serializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
        
        serializerOptions.Converters.Add(new JsonStringEnumConverter());
        serializerOptions.Converters.Add(new VersionOptionsJsonConverter());
        serializerOptions.Converters.Add(new ExpressionJsonConverterFactory());

        var settings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(serializerOptions),
            HttpMessageHandlerFactory = serviceProvider.GetRequiredService<ApiHttpMessageHandler>
        };    
            
        return settings;
    }

}