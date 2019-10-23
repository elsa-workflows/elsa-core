using Elsa.Activities.Console.Activities;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Console.Extensions
{
    public static class ServiceCollectionExtensions
    {        
        public static IServiceCollection AddConsoleActivities(this IServiceCollection services)
        {
            return services
                .AddActivity<ReadLine>()
                .AddActivity<WriteLine>();
        }
    }
}