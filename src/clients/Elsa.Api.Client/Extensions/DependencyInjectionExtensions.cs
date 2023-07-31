using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Api.Client.Contracts;
using Elsa.Api.Client.Converters;
using Elsa.Api.Client.Options;
using Elsa.Api.Client.Resources.ActivityDescriptors.Contracts;
using Elsa.Api.Client.Resources.ActivityExecutions.Contracts;
using Elsa.Api.Client.Resources.Features.Contracts;
using Elsa.Api.Client.Resources.Identity.Contracts;
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
    public static IServiceCollection AddElsaClient(this IServiceCollection services, Action<ElsaClientOptions> configureOptions)
    {
        services.Configure(configureOptions);
        services.AddScoped<IElsaClient, ElsaClient>();
        services.AddActivityTypeService();

        services.AddApi<IWorkflowDefinitionsApi>(CreateRefitSettings);
        services.AddApi<IWorkflowInstancesApi>(CreateRefitSettings);
        services.AddApi<IActivityDescriptorsApi>(CreateRefitSettings);
        services.AddApi<IActivityExecutionsApi>(CreateRefitSettings);
        services.AddApi<IStorageDriversApi>(CreateRefitSettings);
        services.AddApi<IVariableTypesApi>(CreateRefitSettings);
        services.AddApi<IWorkflowActivationStrategiesApi>(CreateRefitSettings);
        services.AddApi<ILoginApi>(CreateRefitSettings);
        services.AddApi<IFeaturesApi>(CreateRefitSettings);
        return services;
    }

    /// <summary>
    /// Adds activity type services to the service collection.
    /// </summary>
    public static IServiceCollection AddActivityTypeService(this IServiceCollection services)
    {
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

    private static void ConfigureElsaApiHttpClient(IServiceProvider serviceProvider, HttpClient httpClient)
    {
        var options = serviceProvider.GetRequiredService<IOptions<ElsaClientOptions>>().Value;
        httpClient.BaseAddress = options.BaseAddress;
        options.ConfigureHttpClient?.Invoke(httpClient);
    }
    
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
            ContentSerializer = new SystemTextJsonContentSerializer(serializerOptions)
        };    
            
        return settings;
    }

}