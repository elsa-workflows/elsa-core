using System;
using Elsa.Activities.Console;
using Elsa.Activities.Workflows;
using Elsa.Contracts;
using Elsa.Modules.Scheduling.Triggers;
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