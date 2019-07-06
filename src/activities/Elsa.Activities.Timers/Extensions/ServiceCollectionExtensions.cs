using System;
using Elsa.Activities.Timers.Activities;
using Elsa.Activities.Timers.HostedServices;
using Elsa.Activities.Timers.Options;
using Elsa.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Activities.Timers.Extensions
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
                .AddActivity<TimerEvent>();
        }
    }
}