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

/// Provides extension methods for dependency injection.
[PublicAPI]
public static class DependencyInjectionExtensions
{
    /// Adds default Elsa API clients configured to use an API key.
    public static IServiceCollection AddDefaultApiClientsUsingApiKey(this IServiceCollection services, Action<ElsaClientOptions> configureOptions)
    {
        var options = new ElsaClientOptions();
        configureOptions(options);

        return services.AddDefaultApiClients(client =>
        {
            client.BaseAddress = options.BaseAddress;
            client.ApiKey = options.ApiKey;
            client.ConfigureHttpClient = options.ConfigureHttpClient;
        });
    }

    /// Adds default Elsa API clients.
    public static IServiceCollection AddDefaultApiClients(this IServiceCollection services, Action<ElsaClientBuilderOptions> configureClient) =>
         services.AddApiClients(configureClient, builderOptions =>
         {
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
         });

    /// <summary>
    /// Adds an API client to the service collection. Requires AddElsaClient to be called exactly once.
    /// </summary>
    public static IServiceCollection AddApiClient<T>(this IServiceCollection services, Action<ElsaClientBuilderOptions> configureClient) where T : class
        => services.AddApiClients(configureClient, builderOptions => services.AddApi<T>(builderOptions));

    /// Adds the Elsa client to the service collection.
    public static IServiceCollection AddApiClients(this IServiceCollection services, Action<ElsaClientBuilderOptions> configureClient, Action<ElsaClientBuilderOptions>? configureServices)
    {
        var builderOptionsServiceDescriptor = services.FirstOrDefault(x => x.ServiceType == typeof(ElsaClientBuilderOptions));

        if (builderOptionsServiceDescriptor == null)
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

            configureServices?.Invoke(builderOptions);
        }

        return services;
    }

    /// <summary>
    /// Adds a refit client for the specified API type.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="httpClientBuilderOptions">An options object that can be used to configure the HTTP client builder.</param>
    /// <typeparam name="T">The type representing the API.</typeparam>
    public static IServiceCollection AddApi<T>(this IServiceCollection services, ElsaClientBuilderOptions? httpClientBuilderOptions = default) where T : class => services.AddApi(typeof(T), httpClientBuilderOptions);

    /// <summary>
    /// Adds a refit client for the specified API type.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="apiType">The type representing the API</param>
    /// <param name="httpClientBuilderOptions">An options object that can be used to configure the HTTP client builder.</param>
    public static IServiceCollection AddApi(this IServiceCollection services, Type apiType, ElsaClientBuilderOptions? httpClientBuilderOptions = default)
    {
        var builder = services.AddRefitClient(apiType, _ => CreateRefitSettings(), apiType.Name).ConfigureHttpClient(ConfigureElsaApiHttpClient);
        httpClientBuilderOptions?.ConfigureHttpClientBuilder(builder);
        httpClientBuilderOptions?.ConfigureRetryPolicy?.Invoke(builder);
        return services;
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

    /// Creates an API client for the specified API type.
    public static T CreateApi<T>(this IServiceProvider serviceProvider, Uri baseAddress) where T : class
    {
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient(typeof(T).Name);
        httpClient.BaseAddress = baseAddress;
        return CreateApi<T>(serviceProvider, httpClient);
    }

    /// Creates an API client for the specified API type.
    public static T CreateApi<T>(this IServiceProvider serviceProvider, HttpClient httpClient) where T : class
        => RestService.For<T>(httpClient, CreateRefitSettings());

    private static void ConfigureElsaApiHttpClient(IServiceProvider serviceProvider, HttpClient httpClient)
    {
        var options = serviceProvider.GetRequiredService<IOptions<ElsaClientOptions>>().Value;
        httpClient.BaseAddress = options.BaseAddress;
        options.ConfigureHttpClient?.Invoke(serviceProvider, httpClient);
    }
}