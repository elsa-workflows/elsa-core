using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Activities;
using Elsa.Activities.Telnyx.Client.Services;
using Elsa.Activities.Telnyx.Options;
using Elsa.Activities.Telnyx.Providers.ActivityTypes;
using Elsa.Activities.Telnyx.Providers.Bookmarks;
using Elsa.Activities.Telnyx.Scripting.JavaScript;
using Elsa.Activities.Telnyx.Scripting.Liquid;
using Elsa.Activities.Telnyx.Webhooks.Filters;
using Elsa.Activities.Telnyx.Webhooks.Handlers;
using Elsa.Activities.Telnyx.Webhooks.Services;
using Elsa.Options;
using Elsa.Scripting.Liquid.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NodaTime;
using NodaTime.Serialization.JsonNet;
using Refit;

namespace Elsa.Activities.Telnyx.Extensions
{
    public static class SetupExtensions
    {
        public static ElsaOptionsBuilder AddTelnyx(
            this ElsaOptionsBuilder elsaOptions, 
            Action<TelnyxOptions>? configure = default, 
            Func<IServiceProvider, HttpClient>? httpClientFactory = default,
            Action<IHttpClientBuilder>? configureHttpClientBuilder = default)
        {
            var services = elsaOptions.Services;

            // Configure Telnyx.
            var telnyxOptions = services.GetTelnyxOptions();
            configure?.Invoke(telnyxOptions);

            // Activities.
            elsaOptions
                .AddActivitiesFrom<AnswerCall>();

            // Services.
            services
                .AddActivityTypeProvider<NotificationActivityTypeProvider>()
                .AddBookmarkProvidersFrom<NotificationBookmarkProvider>()
                .AddNotificationHandlers(typeof(TriggerWebhookActivities))
                .AddJavaScriptTypeDefinitionProvider<TelnyxTypeDefinitionProvider>()
                .AddScoped<IWebhookHandler, WebhookHandler>()
                .AddSingleton<IWebhookFilterService, WebhookFilterService>()
                .AddSingleton<IWebhookFilter, AttributeBasedWebhookFilter>()
                .AddSingleton<IWebhookFilter, CallInitiatedWebhookFilter>()
                .AddScoped(telnyxOptions.ExtensionProviderFactory);
            
            // Liquid.
            services.RegisterLiquidTag(parser => parser.RegisterEmptyTag("telnyx_client_state", TelnyxClientStateTag.WriteToAsync));

            // Telnyx API Client.
            var refitSettings = CreateRefitSettings();
            
            services
                .AddApiClient<ICallsApi>(refitSettings, httpClientFactory, configureHttpClientBuilder)
                .AddApiClient<INumberLookupApi>(refitSettings, httpClientFactory, configureHttpClientBuilder)
                .AddTransient<ITelnyxClient, TelnyxClient>();

            return elsaOptions;
        }

        public static IEndpointConventionBuilder MapTelnyxWebhook(this IEndpointRouteBuilder endpoints, string routePattern = "telnyx-hook")
        {
            return endpoints.MapPost(routePattern, HandleTelnyxRequest);
        }

        public static IServiceCollection AddApiClient<T>(
            this IServiceCollection services, 
            RefitSettings refitSettings, 
            Func<IServiceProvider, HttpClient>? httpClientFactory, 
            Action<IHttpClientBuilder>? configureHttpClientBuilder) where T : class
        {
            if (httpClientFactory == null)
            {
                var httpClientBuilder = services.AddRefitClient<T>(refitSettings).ConfigureHttpClient((sp, client) =>
                {
                    var options = sp.GetRequiredService<TelnyxOptions>();
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
                    var options = sp.GetRequiredService<TelnyxOptions>();
                    httpClient.BaseAddress ??= options.ApiUrl;
                    httpClient.DefaultRequestHeaders.Authorization ??= new AuthenticationHeaderValue("Bearer", options.ApiKey);

                    return RestService.For<T>(httpClient, refitSettings);
                });
            }

            return services;
        }

        public static RefitSettings CreateRefitSettings()
        {
            var serializerSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                }
            };

            serializerSettings.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);

            return new RefitSettings
            {
                ContentSerializer = new NewtonsoftJsonContentSerializer(serializerSettings)
            };
        }

        private static TelnyxOptions GetTelnyxOptions(this IServiceCollection services)
        {
            var telnyxOptions = (TelnyxOptions?) services.FirstOrDefault(x => x.ServiceType == typeof(TelnyxOptions))?.ImplementationInstance;

            if (telnyxOptions == null)
            {
                telnyxOptions = new TelnyxOptions();
                services.AddSingleton(telnyxOptions);
            }

            return telnyxOptions;
        }

        private static async Task HandleTelnyxRequest(HttpContext context)
        {
            var services = context.RequestServices;
            var webhookHandler = services.GetRequiredService<IWebhookHandler>();
            await webhookHandler.HandleAsync(context);
        }
    }
}