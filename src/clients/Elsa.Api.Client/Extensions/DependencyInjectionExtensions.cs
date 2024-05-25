using Elsa.Api.Client.Options;
using Elsa.Api.Client.Resources.ActivityDescriptorOptions.Contracts;
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
using Elsa.Api.Client.Resources.WorkflowExecutionContexts.Contracts;
using Elsa.Api.Client.Resources.WorkflowInstances.Contracts;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Refit;
using static Elsa.Api.Client.RefitSettingsHelper;

namespace Elsa.Api.Client.Extensions;

/// <summary>
/// Provides extension methods for dependency injection.
/// </summary>
[PublicAPI]
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Adds the Elsa API client configured to use an API key to the service collection.
    /// </summary>
    public static IServiceCollection AddElsaApiKeyClient(this IServiceCollection services, Action<ElsaClientOptions> configureOptions)
    {
        var options = new ElsaClientOptions();
        configureOptions(options);

        return services.AddElsaClient(client =>
        {
            client.BaseAddress = options.BaseAddress;
            client.ApiKey = options.ApiKey;
            client.ConfigureHttpClient = options.ConfigureHttpClient;
        });
    }

    /// <summary>
    /// Adds the Elsa client to the service collection.
    /// </summary>
    public static IServiceCollection AddElsaClient(this IServiceCollection services, Action<ElsaClientBuilderOptions> configureClient)
    {
        var builderOptions = new ElsaClientBuilderOptions();
        configureClient.Invoke(builderOptions);
        builderOptions.ConfigureHttpClientBuilder += builder => builder.AddHttpMessageHandler(sp => (DelegatingHandler)sp.GetRequiredService(builderOptions.AuthenticationHandler));

        services.AddScoped(builderOptions.AuthenticationHandler);

        services.Configure<ElsaClientOptions>(options =>
        {
            options.BaseAddress = builderOptions.BaseAddress;
            options.ConfigureHttpClient = builderOptions.ConfigureHttpClient;
            options.ApiKey = builderOptions.ApiKey;
        });

        var builderOptionsWithoutRetryPolicy = new ElsaClientBuilderOptions
        {
            ApiKey = builderOptions.ApiKey,
            AuthenticationHandler = builderOptions.AuthenticationHandler,
            BaseAddress = builderOptions.BaseAddress,
            ConfigureHttpClient = builderOptions.ConfigureHttpClient,
            ConfigureHttpClientBuilder = builderOptions.ConfigureHttpClientBuilder,
            ConfigureRetryPolicy = null
        };

        services.AddApi<IWorkflowDefinitionsApi>(builderOptions);
        services.AddApi<IExecuteWorkflowApi>(builderOptionsWithoutRetryPolicy);
        services.AddApi<IWorkflowInstancesApi>(builderOptions);
        services.AddApi<IActivityDescriptorsApi>(builderOptions);
        services.AddApi<IActivityDescriptorOptionsApi>(builderOptions);
        services.AddApi<IActivityExecutionsApi>(builderOptions);
        services.AddApi<IStorageDriversApi>(builderOptions);
        services.AddApi<IVariableTypesApi>(builderOptions);
        services.AddApi<IWorkflowActivationStrategiesApi>(builderOptions);
        services.AddApi<IIncidentStrategiesApi>(builderOptions);
        services.AddApi<ILoginApi>(builderOptions);
        services.AddApi<IFeaturesApi>(builderOptions);
        services.AddApi<IJavaScriptApi>(builderOptions);
        services.AddApi<IExpressionDescriptorsApi>(builderOptions);
        services.AddApi<IWorkflowContextProviderDescriptorsApi>(builderOptions);
        return services;
    }

    /// <summary>
    /// Adds a refit client for the specified API type.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="httpClientBuilderOptions">An options object that can be used to configure the HTTP client builder.</param>
    /// <typeparam name="T">The type representing the API.</typeparam>
    public static void AddApi<T>(this IServiceCollection services, ElsaClientBuilderOptions? httpClientBuilderOptions = default) where T : class
    {
        var builder = services.AddRefitClient<T>(_ => CreateRefitSettings(), typeof(T).Name).ConfigureHttpClient(ConfigureElsaApiHttpClient);
        httpClientBuilderOptions?.ConfigureHttpClientBuilder(builder);
        httpClientBuilderOptions?.ConfigureRetryPolicy?.Invoke(builder);
    }

    /// <summary>
    /// Adds a refit client for the specified API type.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="httpClientBuilderOptions">An options object that can be used to configure the HTTP client builder.</param>
    /// <typeparam name="T">The type representing the API.</typeparam>
    public static void AddApiWithoutRetryPolicy<T>(this IServiceCollection services, ElsaClientBuilderOptions? httpClientBuilderOptions = default) where T : class
    {
        var builder = services
            .AddRefitClient<T>(_ => CreateRefitSettings(), typeof(T).Name)
            .ConfigureHttpClient(ConfigureElsaApiHttpClient);
        httpClientBuilderOptions?.ConfigureHttpClientBuilder(builder);
    }

    /// <summary>
    /// Creates an API client for the specified API type.
    /// </summary>
    public static T CreateApi<T>(this IServiceProvider serviceProvider, Uri baseAddress) where T : class
    {
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient(typeof(T).Name);
        httpClient.BaseAddress = baseAddress;
        return CreateApi<T>(serviceProvider, httpClient);
    }

    /// <summary>
    /// Creates an API client for the specified API type.
    /// </summary>
    public static T CreateApi<T>(this IServiceProvider serviceProvider, HttpClient httpClient) where T : class
    {
        return RestService.For<T>(httpClient, CreateRefitSettings());
    }

    private static void ConfigureElsaApiHttpClient(IServiceProvider serviceProvider, HttpClient httpClient)
    {
        var options = serviceProvider.GetRequiredService<IOptions<ElsaClientOptions>>().Value;
        httpClient.BaseAddress = options.BaseAddress;
        options.ConfigureHttpClient?.Invoke(serviceProvider, httpClient);
    }
}