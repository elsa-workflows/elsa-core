using System;
using Elsa.Activities.Cron.Activities;
using Elsa.Activities.Cron.HostedServices;
using Elsa.Activities.Cron.Options;
using Elsa.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Activities.Cron.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCronActivities(this IServiceCollection services, Action<OptionsBuilder<CronOptions>> options = null)
        {
            var optionsBuilder = services.AddOptions<CronOptions>();
            options?.Invoke(optionsBuilder);
            
            return services
                .AddOptions()
                .AddHostedService<CronHostedService>()
                .AddActivity<CronTrigger>();
        }
    }
}