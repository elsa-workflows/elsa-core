using System;
using Elsa;
using Elsa.Activities.Timers;
using Elsa.Activities.Timers.Handlers;
using Elsa.Activities.Timers.Options;
using Elsa.Activities.Timers.StartupTasks;
using Elsa.Activities.Timers.Triggers;
using Elsa.Runtime;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptions AddTimerActivities(this ElsaOptions options, Action<TimersOptions>? configure = default)
        {
            var timersOptions = new TimersOptions(options.Services);
            configure?.Invoke(timersOptions);

            options.Services
                .AddNotificationHandlers(typeof(RemoveScheduledTriggers))
                .AddStartupTask<StartJobs>()
                .AddTriggerProvider<TimerTriggerProvider>()
                .AddTriggerProvider<CronTriggerProvider>()
                .AddTriggerProvider<StartAtTriggerProvider>();

            return options
                .AddActivity<Cron>()
                .AddActivity<Timer>()
                .AddActivity<StartAt>()
                .AddActivity<ClearTimer>();
        }
    }
}