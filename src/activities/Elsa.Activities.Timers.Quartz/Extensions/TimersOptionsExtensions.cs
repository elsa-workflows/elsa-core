using System;

using Elsa.Activities.Timers.Options;
using Elsa.Activities.Timers.Quartz.Jobs;
using Elsa.Activities.Timers.Quartz.Services;
using Elsa.Activities.Timers.Services;

using Microsoft.Extensions.DependencyInjection;

using Quartz;

namespace Elsa.Activities.Timers
{
    public static class TimersOptionsExtensions
    {
        /// <summary>
        /// Add Quartz for background processing
        /// </summary>
        /// <param name="timersOptions"></param>
        public static void UseQuartzProvider(this TimersOptions timersOptions, Action<QuartzOptions>? configureOptions = default, Action<IServiceCollectionQuartzConfigurator>? configureQuartz = default)
        {
            if (configureOptions != null)
                timersOptions.Services.Configure(configureOptions);
            else
                timersOptions.Services.AddOptions<QuartzOptions>();

            timersOptions.Services.AddQuartz(configure => ConfigureQuartz(configure, configureQuartz))
                .AddQuartzHostedService(ConfigureQuartzHostedService)
                .AddSingleton<IWorkflowScheduler, QuartzWorkflowScheduler>()
                .AddSingleton<ICrontabParser, QuartzCrontabParser>()
                .AddTransient<RunWorkflowJob>();
        }

        private static void ConfigureQuartzHostedService(QuartzHostedServiceOptions options)
        {
            options.WaitForJobsToComplete = true;
        }

        private static void ConfigureQuartz(IServiceCollectionQuartzConfigurator quartz, Action<IServiceCollectionQuartzConfigurator>? configureQuartz)
        {
            quartz.UseMicrosoftDependencyInjectionScopedJobFactory(options => options.AllowDefaultConstructor = true);
            quartz.AddJob<RunWorkflowJob>(job => job.StoreDurably().WithIdentity(nameof(RunWorkflowJob)));

            if (configureQuartz != null)
            {
                configureQuartz(quartz);
            }
            else
            {
                quartz.UseSimpleTypeLoader();
                quartz.UseInMemoryStore();
            }
        }
    }
}
