using Elsa.Activities.Primitives.Drivers;
using Elsa.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Primitives.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPrimitiveWorkflowDescriptors(this IServiceCollection services)
        {
            return services.AddActivityDescriptors<ActivityDescriptors>();
        }
        
        public static IServiceCollection AddPrimitiveWorkflowDrivers(this IServiceCollection services)
        {
            return services
                .AddPrimitiveWorkflowDescriptors()
                .AddActivityDriver<SetVariableDriver>()
                .AddActivityDriver<ForEachDriver>()
                .AddActivityDriver<ForkDriver>()
                .AddActivityDriver<IfElseDriver>();
        }
    }
}