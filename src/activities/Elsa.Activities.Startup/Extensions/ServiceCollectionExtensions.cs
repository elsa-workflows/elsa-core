using Elsa.Activities.Startup.Activities;
using Elsa.Activities.Startup.HostedServices;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Startup.Extensions
{
    public static class ServiceCollectionExtensions
    {        
        public static IServiceCollection AddStartupActivities(this IServiceCollection services)
        {
            return services
                .AddHostedService<StartupHostedService>()
                .AddActivity<Activities.Startup>();
        }
    }
}