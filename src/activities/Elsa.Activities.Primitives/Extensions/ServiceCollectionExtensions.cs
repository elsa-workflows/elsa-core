using Elsa.Activities.Primitives.Drivers;
using Elsa.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Primitives.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPrimitiveActivities(this IServiceCollection services)
        {
            return services
                .AddActivity<SetVariableDriver>()
                .AddActivity<ForEachDriver>()
                .AddActivity<ForkDriver>()
                .AddActivity<JoinDriver>()
                .AddSingleton<IWorkflowEventHandler>(sp => sp.GetRequiredService<JoinDriver>())
                .AddActivity<IfElseDriver>()
                .AddActivity<SwitchDriver>();
        }
    }
}