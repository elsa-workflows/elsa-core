using System;
using Elsa.Activities.Timers;
using Elsa.Activities.Timers.HostedServices;
using Elsa.Activities.Timers.Jobs;
using Elsa.Activities.Timers.Options;
using Elsa.Activities.Timers.Services;
using Elsa.Activities.Timers.Triggers;
using Quartz;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddTimerActivities(this IServiceCollection services, Action<TimersOptions>? configureOptions = null)
        {
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }

            return services
                .AddQuartz(ConfigureQuartz)
                .AddQuartzHostedService(ConfigureQuartzHostedService)
                .AddSingleton<IWorkflowScheduler, WorkflowScheduler>()
                .AddTransient<RunWorkflowJob>()
                .AddHostedService<StartJobs>()
                .AddActivity<CronEvent>()
                .AddActivity<TimerEvent>()
                .AddActivity<StartAt>()
                .AddTriggerProvider<TimerEventTriggerProvider>()
                .AddTriggerProvider<CronEventTriggerProvider>()
                .AddTriggerProvider<StartAtTriggerProvider>();
        }

        private static void ConfigureQuartzHostedService(QuartzHostedServiceOptions options)
        {
            options.WaitForJobsToComplete = true;
        }

        private static void ConfigureQuartz(IServiceCollectionQuartzConfigurator quartz)
        {
            quartz.SchedulerId = "TimerEvent";
            quartz.UseMicrosoftDependencyInjectionScopedJobFactory(options => options.AllowDefaultConstructor = true);
            quartz.UseSimpleTypeLoader();
            quartz.UseInMemoryStore();
        }
    }
}