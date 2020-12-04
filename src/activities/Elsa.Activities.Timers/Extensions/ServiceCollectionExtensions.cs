using System;
using Elsa.Activities.Timers;
using Elsa.Activities.Timers.HostedServices;
using Elsa.Activities.Timers.Jobs;
using Elsa.Activities.Timers.Services;
using Elsa.Activities.Timers.Triggers;
using Quartz;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddTimerActivities(this IServiceCollection services, Action<QuartzOptions>? configureOptions = default, Action<IServiceCollectionQuartzConfigurator>? configureQuartz = default)
        {
            if (configureOptions != null)
                services.Configure(configureOptions);
            else
                services.AddOptions<QuartzOptions>();

            return services
                .AddQuartz(configure => ConfigureQuartz(configure, configureQuartz))
                .AddQuartzHostedService(ConfigureQuartzHostedService)
                .AddSingleton<IWorkflowScheduler, WorkflowScheduler>()
                .AddTransient<RunWorkflowJob>()
                .AddHostedService<StartJobs>()
                .AddActivity<CronEvent>()
                .AddActivity<Timer>()
                .AddActivity<StartAt>()
                .AddTriggerProvider<TimerEventTriggerProvider>()
                .AddTriggerProvider<CronEventTriggerProvider>()
                .AddTriggerProvider<StartAtTriggerProvider>();
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