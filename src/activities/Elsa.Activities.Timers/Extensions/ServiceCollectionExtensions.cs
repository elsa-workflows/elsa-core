using System;
using Elsa.Activities.Timers.Activities;
using Elsa.Activities.Timers.HostedServices;
using Elsa.Activities.Timers.Options;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddTimerActivities(this IServiceCollection services, Action<OptionsBuilder<TimersOptions>> options = null)
        {
            var optionsBuilder = services.AddOptions<TimersOptions>();
            options?.Invoke(optionsBuilder);
            
            return services
                .AddOptions()
                .AddHostedService<TimersHostedService>()
                .AddActivity<CronEvent>()
                .AddActivity<TimerEvent>()
                .AddActivity<InstantEvent>();
        }
    }
}