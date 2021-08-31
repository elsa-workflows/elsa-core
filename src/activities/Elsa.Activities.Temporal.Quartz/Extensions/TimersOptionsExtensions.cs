using System;
using Elsa.Activities.Temporal.Common.Options;
using Elsa.Activities.Temporal.Common.Services;
using Elsa.Activities.Temporal.Quartz.Handlers;
using Elsa.Activities.Temporal.Quartz.Jobs;
using Elsa.Activities.Temporal.Quartz.Services;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

// ReSharper disable once CheckNamespace
namespace Elsa
{
    public static class TimersOptionsExtensions
    {
        /// <summary>
        /// Add Elsa Quartz services and Quartz services.
        /// </summary>
        /// <param name="timersOptions">The timer options being configured.</param>
        /// <param name="registerQuartz">True to automatically register Quartz services. When false, make sure to register Quartz yourself.</param>
        /// <param name="configureQuartzOptions">When <see cref="registerQuartz"/> is true, you can use this callback to further configure Quartz options.</param>
        /// <param name="configureQuartz">When <see cref="registerQuartz"/> is true, you can use this callback to further configure Quartz.</param>
        /// <param name="configureQuartzHostedService">When <see cref="registerQuartz"/> is true, you can use this callback to further configure the Quartz hosted service.</param>
        public static void UseQuartzProvider(
            this TimersOptions timersOptions,
            bool registerQuartz = true,
            Action<QuartzOptions>? configureQuartzOptions = default,
            Action<IServiceCollectionQuartzConfigurator>? configureQuartz = default,
            Action<QuartzHostedServiceOptions>? configureQuartzHostedService = default)
        {
            timersOptions.Services
                .AddSingleton<QuartzSchedulerProvider>()
                .AddSingleton<IWorkflowDefinitionScheduler, QuartzWorkflowDefinitionScheduler>()
                .AddSingleton<IWorkflowInstanceScheduler, QuartzWorkflowInstanceScheduler>()
                .AddSingleton<ICrontabParser, QuartzCrontabParser>()
                .AddTransient<RunQuartzWorkflowDefinitionJob>()
                .AddTransient<RunQuartzWorkflowInstanceJob>()
                .AddNotificationHandlers(typeof(ConfigureCronProperty));

            if (registerQuartz)
            {
                if (configureQuartzOptions != null)
                    timersOptions.Services.Configure(configureQuartzOptions);
                
                timersOptions.Services
                    .AddQuartz(configure => ConfigureQuartz(configure, configureQuartz))
                    .AddQuartzHostedService(options => ConfigureQuartzHostedService(options, configureQuartzHostedService));
            }
        }

        private static void ConfigureQuartzHostedService(QuartzHostedServiceOptions options, Action<QuartzHostedServiceOptions>? configureQuartzHostedService)
        {
            options.WaitForJobsToComplete = true;
            configureQuartzHostedService?.Invoke(options);
        }

        private static void ConfigureQuartz(IServiceCollectionQuartzConfigurator quartz, Action<IServiceCollectionQuartzConfigurator>? configureQuartz)
        {
            quartz.UseMicrosoftDependencyInjectionJobFactory();
            quartz.AddJob<RunQuartzWorkflowDefinitionJob>(job => job.StoreDurably().WithIdentity(nameof(RunQuartzWorkflowDefinitionJob)));
            quartz.AddJob<RunQuartzWorkflowInstanceJob>(job => job.StoreDurably().WithIdentity(nameof(RunQuartzWorkflowInstanceJob)));
            quartz.UseSimpleTypeLoader();
            quartz.UseInMemoryStore();
            configureQuartz?.Invoke(quartz);
        }
    }
}