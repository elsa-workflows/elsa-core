using System;
using Autofac;
using Elsa.Activities.Conductor.Consumers;
using Elsa.Activities.Conductor.Models;
using Elsa.Activities.Conductor.Options;
using Elsa.Activities.Conductor.Providers.ActivityTypes;
using Elsa.Activities.Conductor.Providers.Bookmarks;
using Elsa.Activities.Conductor.Providers.Commands;
using Elsa.Activities.Conductor.Providers.Events;
using Elsa.Activities.Conductor.Providers.Tasks;
using Elsa.Activities.Conductor.Services;
using Elsa.Extensions;
using Elsa.Options;
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

            services.ConfigureCommandsHttpClient(configureHttpClient);
            services.ConfigureTasksHttpClient(configureHttpClient);

            elsa.ContainerBuilder
                .AddCommandsProvider<OptionsCommandsProvider>()
                .AddEventsProvider<OptionsEventsProvider>()
                .AddTasksProvider<OptionsTasksProvider>()
                .AddActivityTypeProvider<CommandActivityTypeProvider>()
                .AddActivityTypeProvider<EventActivityTypeProvider>()
                .AddActivityTypeProvider<TaskActivityTypeProvider>()
                .AddBookmarkProvider<EventBookmarkProvider>()
                .AddBookmarkProvider<TaskBookmarkProvider>();

            elsa
                .AddActivitiesFrom<SendCommand>()
                .AddCompetingConsumer<SendCommandConsumer, SendCommandModel>("ConductorCommand")
                .AddCompetingConsumer<RunTaskConsumer, RunTaskModel>("ConductorCommand");

            return elsa;
        }

        public static ContainerBuilder AddCommandsProvider<T>(this ContainerBuilder services) where T : class, ICommandsProvider => services.AddScoped<ICommandsProvider, T>();
        public static ContainerBuilder AddEventsProvider<T>(this ContainerBuilder services) where T : class, IEventsProvider => services.AddScoped<IEventsProvider, T>();
        public static ContainerBuilder AddTasksProvider<T>(this ContainerBuilder services) where T : class, ITasksProvider => services.AddScoped<ITasksProvider, T>();

        private static void ConfigureCommandsHttpClient(this IServiceCollection services, Action<IHttpClientBuilder>? configureHttpClient = default) => services.ConfigureHttpClient<ApplicationCommandsClient>(o => o.CommandsHookUrl, configureHttpClient);
        private static void ConfigureTasksHttpClient(this IServiceCollection services, Action<IHttpClientBuilder>? configureHttpClient = default) => services.ConfigureHttpClient<ApplicationTasksClient>(o => o.CommandsHookUrl, configureHttpClient);

        private static void ConfigureHttpClient<T>(this IServiceCollection services, Func<ConductorOptions, Uri> baseAddress, Action<IHttpClientBuilder>? configureHttpClient = default) where T : class
        {
            var httpClientBuilder = services.AddHttpClient<T>((sp, httpClient) =>
            {
                var options = sp.GetRequiredService<IOptions<ConductorOptions>>().Value;
                httpClient.BaseAddress = baseAddress(options);
            });

            if (configureHttpClient == null)
                httpClientBuilder.AddTransientHttpErrorPolicy(x => x.WaitAndRetryAsync(10, retryCount => TimeSpan.FromSeconds(Math.Pow(2, retryCount))));
            else
                configureHttpClient(httpClientBuilder);
        }
    }
}