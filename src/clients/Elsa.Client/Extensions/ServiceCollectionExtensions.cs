using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Client.Converters;
using Elsa.Client.Options;
using Elsa.Client.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using Refit;

namespace Elsa.Client.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddElsaClient(this IServiceCollection services, Action<ElsaClientOptions>? configure = default)
        {
            if (configure != null)
                services.Configure(configure);
            else
                services.ConfigureOptions<ElsaClientOptions>();

            var jsonSerializerSettings = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            jsonSerializerSettings.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
            jsonSerializerSettings.Converters.Add(new JsonStringEnumConverter());
            jsonSerializerSettings.Converters.Add(new TypeConverter());
            jsonSerializerSettings.Converters.Add(new VersionOptionsConverter());
            
            var refitSettings = new RefitSettings
            {
                ContentSerializer = new SystemTextJsonContentSerializer(jsonSerializerSettings)
            };
            
            services.AddRefitClient<IWorkflowDefinitionsApi>(refitSettings).ConfigureHttpClient((sp, client) =>
            {
                var serverUrl = sp.GetRequiredService<IOptions<ElsaClientOptions>>().Value.ServerUrl;
                client.BaseAddress = serverUrl;
            }).AddHttpMessageHandler<Spy>().AddHttpMessageHandler();

            services.AddTransient<Spy>();
            
            return services
                .AddTransient<IElsaClient, ElsaClient>();
        }
    }

    public class Spy : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);
            return response;
        }
    }
}