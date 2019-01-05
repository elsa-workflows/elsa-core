using Elsa.Activities.Console.Descriptors;
using Elsa.Activities.Console.Drivers;
using Elsa.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Console.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddConsoleDescriptors(this IServiceCollection services)
        {
            return services
                .AddActivityDescriptor<ReadLineDescriptor>()
                .AddActivityDescriptor<WriteLineDescriptor>();
        }
        
        public static IServiceCollection AddConsoleDrivers(this IServiceCollection services)
        {
            return services
                .AddActivityDriver<ReadLineDriver>()
                .AddActivityDriver<WriteLineDriver>();
        }
    }
}