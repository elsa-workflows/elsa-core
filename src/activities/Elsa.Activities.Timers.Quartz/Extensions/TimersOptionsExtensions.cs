using System;
using Elsa.Activities.Timers.Options;
using Elsa.Activities.Timers.Quartz.Jobs;
using Elsa.Activities.Timers.Quartz.Services;
using Elsa.Activities.Timers.Services;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

// ReSharper disable once CheckNamespace
namespace Elsa
{
    public static class TimersOptionsExtensions
    {
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
                .AddTransient<RunQuartzWorkflowJob>();
        }

        private static void ConfigureQuartzHostedService(QuartzHostedServiceOptions options)
        {
            options.WaitForJobsToComplete = true;
        }

        private static void ConfigureQuartz(IServiceCollectionQuartzConfigurator quartz, Action<IServiceCollectionQuartzConfigurator>? configureQuartz)
        {
            quartz.UseMicrosoftDependencyInjectionScopedJobFactory(options => options.AllowDefaultConstructor = true);
            quartz.AddJob<RunQuartzWorkflowJob>(job => job.StoreDurably().WithIdentity(nameof(RunQuartzWorkflowJob)));

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