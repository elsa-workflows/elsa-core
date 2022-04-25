using Elsa.Activities;
using Elsa.Modules.Activities.Console;
using Elsa.Modules.Scheduling.Activities;
using Elsa.Services;

namespace Elsa.Samples.Web1.Workflows;

public class DelayWorkflow : IWorkflow
{
    public void Build(IWorkflowDefinitionBuilder workflow)
    {
        workflow.WithRoot(new Sequence
        {
            Activities =
            {
                new WriteLine("Sleeping for 5 seconds..."),
                Delay.FromSeconds(5),
                new WriteLine(context => $"Continuing at {context.GetRequiredService<ISystemClock>().UtcNow}")
            }
        });
    }
}