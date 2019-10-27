using Elsa.Activities.ControlFlow.Activities;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.ControlFlow.Extensions
{
    public static class ControlFlowServiceCollectionExtensions
    {
        public static IServiceCollection AddControlFlowActivities(this IServiceCollection services)
        {
            return services
                .AddActivity<ForEach>()
                .AddActivity<While>()
                .AddActivity<Fork>()
                .AddActivity<Join>()
                .AddSingleton<IWorkflowEventHandler>(sp => sp.GetRequiredService<Join>())
                .AddActivity<IfElse>()
                .AddActivity<Switch>();
        }
    }
}