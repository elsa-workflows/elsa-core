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
        public static IServiceCollection AddCronDescriptors(this IServiceCollection services)
        {
            return services.AddActivityDescriptors<ActivityDescriptors>();
        }

        public static IServiceCollection AddCronActivities(this IServiceCollection services, IConfiguration configuration)
        {
            return services
                .AddCronDescriptors()
                .AddOptions()
                .Configure<CronOptions>(configuration)
                .AddHostedService<CronHostedService>()
                .AddActivityDriver<CronTriggerDriver>();
        }
    }
}