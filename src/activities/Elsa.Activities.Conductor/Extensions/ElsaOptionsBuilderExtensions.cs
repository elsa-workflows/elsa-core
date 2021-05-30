using System;
using Elsa.Activities.Conductor.Consumers;
using Elsa.Activities.Conductor.Models;
using Elsa.Activities.Conductor.Options;
using Elsa.Activities.Conductor.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;

namespace Elsa.Activities.Conductor.Extensions
{
    public static class ElsaOptionsBuilderExtensions
    {
        public static ElsaOptionsBuilder AddConductorActivities(this ElsaOptionsBuilder elsa, Action<ConductorOptions>? configureOptions = default, Action<IHttpClientBuilder>? configureHttpClient = default)
        {
            var services = elsa.Services;

            if (configureOptions != null)
                services.Configure(configureOptions);

            var httpClientBuilder = services.AddHttpClient<RemoteApplicationClient>((sp, httpClient) =>
            {
                var options = sp.GetRequiredService<IOptions<ConductorOptions>>().Value;
                httpClient.BaseAddress = options.ApplicationHookUrl;
            });

            if (configureHttpClient == null)
                httpClientBuilder.AddTransientHttpErrorPolicy(x => x.WaitAndRetryAsync(10, retryCount => TimeSpan.FromSeconds(Math.Pow(2, retryCount))));
            else
                configureHttpClient(httpClientBuilder);

            elsa.AddActivitiesFrom<SendCommand>();
            elsa.AddCompetingConsumer<SendCommandConsumer, SendCommandModel>();
            return elsa;
        }
    }
}