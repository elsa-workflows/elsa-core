using Elsa.Activities.Reflection.Activities;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Reflection.Extensions
{
    public static class ServiceCollectionExtensions
    {        
        public static IServiceCollection AddReflectionActivities(this IServiceCollection services)
        {
            return services
                .AddActivity<ExecuteMethod>()
                .AddActivity<SplitObject>();
        }
    }
}