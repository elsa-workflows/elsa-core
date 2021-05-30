using System;
using System.Collections.Generic;
using Elsa.Activities.Conductor.Consumers;
using Elsa.Activities.Conductor.Models;
using Elsa.Activities.Conductor.Options;
using Elsa.Activities.Conductor.Providers.ActivityTypes;
using Elsa.Activities.Conductor.Providers.Bookmarks;
using Elsa.Activities.Conductor.Providers.Commands;
using Elsa.Activities.Conductor.Providers.Events;
using Elsa.Activities.Conductor.Services;
using Elsa.Services;
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

            services.ConfigureHttpClient(configureHttpClient);

            services
                .AddCommandProvider<OptionsCommandProvider>()
                .AddEventProvider<OptionsEventProvider>()
                .AddActivityTypeProvider<CommandActivityTypeProvider>()
                .AddActivityTypeProvider<EventActivityTypeProvider>()
                .AddBookmarkProvider<EventBookmarkProvider>()
                .AddSingleton<Scoped<IEnumerable<ICommandProvider>>>()
                .AddSingleton<Scoped<IEnumerable<IEventProvider>>>();

            elsa
                .AddActivitiesFrom<SendCommand>()
                .AddCompetingConsumer<SendCommandConsumer, SendCommandModel>();

            return elsa;
        }

        public static IServiceCollection AddCommandProvider<T>(this IServiceCollection services) where T : class, ICommandProvider => services.AddScoped<ICommandProvider, T>();
        public static IServiceCollection AddEventProvider<T>(this IServiceCollection services) where T : class, IEventProvider => services.AddScoped<IEventProvider, T>();

        private static void ConfigureHttpClient(this IServiceCollection services, Action<IHttpClientBuilder>? configureHttpClient = default)
        {
            var httpClientBuilder = services.AddHttpClient<RemoteApplicationClient>((sp, httpClient) =>
            {
                var options = sp.GetRequiredService<IOptions<ConductorOptions>>().Value;
                httpClient.BaseAddress = options.ApplicationHookUrl;
            });

            if (configureHttpClient == null)
                httpClientBuilder.AddTransientHttpErrorPolicy(x => x.WaitAndRetryAsync(10, retryCount => TimeSpan.FromSeconds(Math.Pow(2, retryCount))));
            else
                configureHttpClient(httpClientBuilder);
        }
    }
}