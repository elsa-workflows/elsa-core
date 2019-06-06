using Elsa.Activities.Console.Drivers;
using Elsa.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Console.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddConsoleDesigners(this IServiceCollection services)
        {
            return services
                .AddActivityProvider<ActivityProvider>()
                .AddActivityDesigners<ActivityProvider>();
        }
        
        public static IServiceCollection AddConsoleActivities(this IServiceCollection services)
        {
            return services
                .AddActivityProvider<ActivityProvider>()
                .AddActivityDriver<ReadLineDriver>()
                .AddActivityDriver<WriteLineDriver>();
        }
    }
}