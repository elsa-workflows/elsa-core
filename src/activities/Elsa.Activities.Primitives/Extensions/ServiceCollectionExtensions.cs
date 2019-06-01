using Elsa.Activities.Primitives.Drivers;
using Elsa.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Primitives.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPrimitiveWorkflowDescriptors(this IServiceCollection services)
        {
            return services.AddActivityDesigners<ActivityDesignerProvider>();
        }
        
        public static IServiceCollection AddPrimitiveActivities(this IServiceCollection services)
        {
            return services
                .AddActivityProvider<ActivityProvider>()
                .AddActivityDriver<SetVariableDriver>()
                .AddActivityDriver<ForEachDriver>()
                .AddActivityDriver<ForkDriver>()
                .AddActivityDriver<JoinDriver>()
                .AddSingleton<IWorkflowEventHandler>(sp => sp.GetRequiredService<JoinDriver>())
                .AddActivityDriver<IfElseDriver>()
                .AddActivityDriver<SwitchDriver>();
        }
    }
}