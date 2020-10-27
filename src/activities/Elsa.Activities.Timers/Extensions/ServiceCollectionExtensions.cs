using System;
using Elsa.Activities.Timers;
using Elsa.Activities.Timers.HostedServices;
using Elsa.Activities.Timers.Options;

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
                .AddHostedService<TimersHostedService>()
                .AddActivity<CronEvent>()
                .AddActivity<TimerEvent>()
                .AddActivity<InstantEvent>();
        }
    }
}