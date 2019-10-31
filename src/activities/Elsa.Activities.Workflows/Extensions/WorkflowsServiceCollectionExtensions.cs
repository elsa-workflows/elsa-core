using Elsa.Activities.Workflows.Activities;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Workflows.Extensions
{
    public static class WorkflowsServiceCollectionExtensions
    {
        public static IServiceCollection AddWorkflowActivities(this IServiceCollection services)
        {
            return services
                .AddActivity<TriggerWorkflow>()
                .AddActivity<Correlate>()
                .AddActivity<Signaled>()
                .AddActivity<TriggerSignal>()
                .AddActivity<Start>()
                .AddActivity<Finish>();
        }
    }
}