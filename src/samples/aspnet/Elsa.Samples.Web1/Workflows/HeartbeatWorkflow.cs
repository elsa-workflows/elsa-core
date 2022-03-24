using System;
using Elsa.Activities;
using Elsa.Contracts;
using Elsa.Modules.Activities.Activities.Console;
using Elsa.Modules.Scheduling.Activities;
using Elsa.Runtime.Contracts;

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