using Elsa.Activities.Reflection.Activities;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Messagebus.Rebus
{
    public static class ServiceCollectionExtensions
    {        
        public static IServiceCollection AddReflectionActivities(this IServiceCollection services)
        {
            return services
                .AddActivity<DropMessage>();        }
    }
}