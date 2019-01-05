using Elsa.Activities.Console.Drivers;
using Elsa.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Console.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddConsoleWorkflowDescriptors(this IServiceCollection services)
        {
            return services.AddActivityDescriptors<ActivityDescriptors>();
        }
        
        public static IServiceCollection AddConsoleWorkflowDrivers(this IServiceCollection services)
        {
            return services
                .AddConsoleWorkflowDescriptors()
                .AddActivityDriver<ReadLineDriver>()
                .AddActivityDriver<WriteLineDriver>();
        }
    }
}