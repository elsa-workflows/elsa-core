using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dahomey.Json.NamingPolicies;
using Elsa.Mediator.Extensions;
using Elsa.Telnyx.Client.Implementations;
using Elsa.Telnyx.Client.Services;
using Elsa.Telnyx.Converters;
using Elsa.Telnyx.Handlers;
using Elsa.Telnyx.Options;
using Elsa.Telnyx.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Refit;

namespace Elsa.Telnyx.Extensions
{
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
}