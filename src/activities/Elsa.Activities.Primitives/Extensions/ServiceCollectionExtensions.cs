using Elsa.Activities.Primitives.Drivers;
using Elsa.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Primitives.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPrimitiveDescriptors(this IServiceCollection services)
        {
            return services.AddActivityDescriptors<ActivityDescriptors>();
        }
        
        public static IServiceCollection AddPrimitiveDrivers(this IServiceCollection services)
        {
            return services
                .AddActivityDriver<SetVariableDriver>()
                .AddActivityDriver<ForEachDriver>()
                .AddActivityDriver<IfElseDriver>();
        }
    }
}