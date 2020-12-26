using System;
using System.Net.Http;
using Elsa.Client.Converters;
using Elsa.Client.Options;
using Elsa.Client.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using NodaTime;
using NodaTime.Serialization.JsonNet;
using Refit;
using TypeConverter = Elsa.Client.Converters.TypeConverter;

namespace Elsa.Client.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddElsaClient(this IServiceCollection services, Action<ElsaClientOptions>? configure = default, Func<HttpClient>? httpClientFactory = default)
        {
            if (configure != null)
                services.Configure(configure);
            else
                services.ConfigureOptions<ElsaClientOptions>();

            var serializerSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                TypeNameHandling = TypeNameHandling.Auto,
                ContractResolver = new CamelCasePropertyNamesContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy
                    {
                        ProcessDictionaryKeys = false,
                        ProcessExtensionDataNames = true,
                        OverrideSpecifiedNames = false
                    }
                }
            };

            serializerSettings.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
            serializerSettings.Converters.Add(new StringEnumConverter(new DefaultNamingStrategy()));
            serializerSettings.Converters.Add(new TypeConverter());
            serializerSettings.Converters.Add(new VersionOptionsConverter());

            var refitSettings = new RefitSettings
            {
                ContentSerializer = new NewtonsoftJsonContentSerializer(serializerSettings)
            };

            services
                .AddApiClient<IActivitiesApi>(refitSettings, httpClientFactory)
                .AddApiClient<IWorkflowDefinitionsApi>(refitSettings, httpClientFactory)
                .AddApiClient<IWorkflowRegistryApi>(refitSettings, httpClientFactory)
                .AddApiClient<IWorkflowInstancesApi>(refitSettings, httpClientFactory);

            return services
                .AddTransient<IElsaClient, ElsaClient>();
        }

        private static IServiceCollection AddApiClient<T>(this IServiceCollection services, RefitSettings refitSettings, Func<HttpClient>? httpClientFactory) where T : class
        {
            if (httpClientFactory == null)
            {
                services.AddRefitClient<T>(refitSettings).ConfigureHttpClient((sp, client) =>
                {
                    var serverUrl = sp.GetRequiredService<IOptions<ElsaClientOptions>>().Value.ServerUrl;
                    client.BaseAddress = serverUrl;
                });
            }
            else
            {
                services.AddScoped(_ => RestService.For<T>(httpClientFactory(), refitSettings));
            }

            return services;
        }
    }
}