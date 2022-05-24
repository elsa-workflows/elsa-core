using Elsa.Activities;
using Elsa.Scheduling.Activities;
using Elsa.Services;

namespace Elsa.Samples.Web1.Workflows;

public class DelayWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowDefinitionBuilder workflow)
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