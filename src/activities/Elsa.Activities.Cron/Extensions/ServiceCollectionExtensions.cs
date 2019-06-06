using Elsa.Activities.Cron.Drivers;
using Elsa.Activities.Cron.HostedServices;
using Elsa.Activities.Cron.Options;
using Elsa.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Cron.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCronDesigners(this IServiceCollection services)
        {
            return services
                .AddActivityProvider<ActivityProvider>()
                .AddActivityDesigners<ActivityProvider>();
        }

        public static IServiceCollection AddCronActivities(this IServiceCollection services, IConfiguration configuration)
        {
            return services
                .AddOptions()
                .Configure<CronOptions>(configuration)
                .AddHostedService<CronHostedService>()
                .AddActivityProvider<ActivityProvider>()
                .AddActivityDriver<CronTriggerDriver>();
        }
    }
}