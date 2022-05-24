using System;
using Elsa.Activities;
using Elsa.Scheduling.Activities;
using Elsa.Services;

namespace Elsa.Samples.Web1.Workflows;

public class HeartbeatWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowDefinitionBuilder workflow)
    {
        workflow.WithRoot(new Sequence
        {
            Activities =
            {
                new Timer(TimeSpan.FromSeconds(10))
                {
                    CanStartWorkflow = true
                },
                new WriteLine(context => $"Heartbeat at {context.GetRequiredService<ISystemClock>().UtcNow}")
            }
        });
    }
}