using Elsa.Activities.Console.Drivers;
using Elsa.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Console.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddConsoleDescriptors(this IServiceCollection services)
        {
            return services.AddActivityDescriptors<ActivityDescriptors>();
        }
        
        public static IServiceCollection AddConsoleDrivers(this IServiceCollection services)
        {
            return services
                .AddActivityDriver<ReadLineDriver>()
                .AddActivityDriver<WriteLineDriver>();
        }
    }
}