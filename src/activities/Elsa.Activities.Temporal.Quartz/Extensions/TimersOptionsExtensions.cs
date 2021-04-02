using System;
using Elsa.Activities.Temporal.Common.Options;
using Elsa.Activities.Temporal.Common.Services;
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
        /// Add Elsa Quartz services.
        /// </summary>
        public static void UseQuartzProvider(this TimersOptions timersOptions)
        {
            timersOptions.Services
                .AddSingleton<IWorkflowScheduler, QuartzWorkflowScheduler>()
                .AddSingleton<ICrontabParser, QuartzCrontabParser>()
                .AddTransient<RunQuartzWorkflowJob>();
        }

        /// <summary>
        /// Add Elsa Hangfire Services and Quartz services.
        /// </summary>
        /// <remarks>
        /// Use only if Quartz is not already registered in DI.
        /// </remarks>
        public static void UseQuartzProvider(
            this TimersOptions timersOptions,
            Action<QuartzOptions> configureOptions,
            Action<IServiceCollectionQuartzConfigurator>? configureQuartz = default,
            Action<QuartzHostedServiceOptions>? configureQuartzHostedService = default)
        {
            timersOptions.UseQuartzProvider();
            timersOptions.Services.Configure(configureOptions);

            timersOptions.Services
                .AddQuartz(configure => ConfigureQuartz(configure, configureQuartz))
                .AddQuartzHostedService(options => ConfigureQuartzHostedService(options, configureQuartzHostedService));
        }

        private static void ConfigureQuartzHostedService(QuartzHostedServiceOptions options, Action<QuartzHostedServiceOptions>? configureQuartzHostedService)
        {
            options.WaitForJobsToComplete = true;
            configureQuartzHostedService?.Invoke(options);
        }

        private static void ConfigureQuartz(IServiceCollectionQuartzConfigurator quartz, Action<IServiceCollectionQuartzConfigurator>? configureQuartz)
        {
            quartz.UseMicrosoftDependencyInjectionScopedJobFactory(options => options.AllowDefaultConstructor = true);
            quartz.AddJob<RunQuartzWorkflowJob>(job => job.StoreDurably().WithIdentity(nameof(RunQuartzWorkflowJob)));
            quartz.UseSimpleTypeLoader();
            quartz.UseInMemoryStore();
            
            configureQuartz?.Invoke(quartz);
        }
    }
}