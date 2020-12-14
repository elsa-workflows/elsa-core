using System;

using Elsa.Activities.Timers;
using Elsa.Activities.Timers.HostedServices;
using Elsa.Activities.Timers.Options;
using Elsa.Activities.Timers.Triggers;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddTimerActivities(this IServiceCollection services,
            Action<TimersOptions>? configure = default)
        {
            var options = new TimersOptions(services);
            configure?.Invoke(options);

            return services                
                .AddHostedService<StartJobs>()
                .AddActivity<Cron>()
                .AddActivity<Timer>()
                .AddActivity<StartAt>()
                .AddTriggerProvider<TimerTriggerProvider>()
                .AddTriggerProvider<CronTriggerProvider>()
                .AddTriggerProvider<StartAtTriggerProvider>();
        }
    }
}