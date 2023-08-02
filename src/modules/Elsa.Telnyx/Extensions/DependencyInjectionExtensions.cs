using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Telnyx.Client.Implementations;
using Elsa.Telnyx.Client.Services;
using Elsa.Telnyx.Contracts;
using Elsa.Telnyx.Handlers;
using Elsa.Telnyx.Options;
using Elsa.Telnyx.Serialization;
using Elsa.Telnyx.Services;
using Microsoft.Extensions.Options;
using Refit;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides service dependency extensions that register required services for Telnyx integration.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Adds Telnyx services to the service container.
    /// </summary>
    /// <returns></returns>
    public static IServiceCollection AddTelnyx(
        this IServiceCollection services, 
        Action<TelnyxOptions>? configure = default, 
        Func<IServiceProvider, HttpClient>? httpClientFactory = default,
        Action<IHttpClientBuilder>? configureHttpClientBuilder = default)
    {
        // Telnyx options.
        configure ??= options => options.ApiUrl = new Uri("https://api.telnyx.com");
        services.Configure(configure);

        // Services.
        services
            .AddNotificationHandlersFrom<TriggerWebhookActivities>()
            .AddScoped<IWebhookHandler, WebhookHandler>();
            
        // Telnyx API Client.
        var refitSettings = CreateRefitSettings();
            
        services
            .AddApiClient<ICallsApi>(refitSettings, httpClientFactory, configureHttpClientBuilder)
            .AddApiClient<INumberLookupApi>(refitSettings, httpClientFactory, configureHttpClientBuilder)
            .AddTransient<ITelnyxClient, TelnyxClient>();

        return services;
    }

    /// <summary>
    /// Registers the specified interface type as a Refit client.
    /// </summary>
    private static IServiceCollection AddApiClient<T>(
        this IServiceCollection services, 
        RefitSettings refitSettings, 
        Func<IServiceProvider, HttpClient>? httpClientFactory, 
        Action<IHttpClientBuilder>? configureHttpClientBuilder) where T : class
    {
        if (httpClientFactory == null)
        {
            var httpClientBuilder = services.AddRefitClient<T>(refitSettings).ConfigureHttpClient((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<TelnyxOptions>>().Value;
                client.BaseAddress = options.ApiUrl;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
            });
                
            configureHttpClientBuilder?.Invoke(httpClientBuilder);
        }
        else
        {
            services.AddScoped(sp =>
            {
                var httpClient = httpClientFactory(sp);
                var options = sp.GetRequiredService<IOptions<TelnyxOptions>>().Value;
                httpClient.BaseAddress ??= options.ApiUrl;
                httpClient.DefaultRequestHeaders.Authorization ??= new AuthenticationHeaderValue("Bearer", options.ApiKey);

                return RestService.For<T>(httpClient, refitSettings);
            });
        }

        return services;
    }

    private static RefitSettings CreateRefitSettings()
    {
        var serializerSettings = new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = new SnakeCaseNamingPolicy(),
        };
            
        serializerSettings.Converters.Add(new WebhookDataJsonConverter());

        return new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(serializerSettings)
        };
    }
}