using System;
using Elsa.Activities;
using Elsa.Contracts;
using Elsa.Modules.Activities.Console;
using Elsa.Modules.Scheduling.Activities;

namespace Elsa.Samples.Web1.Workflows;

public class HeartbeatWorkflow : IWorkflow
{
    public void Build(IWorkflowDefinitionBuilder workflow)
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