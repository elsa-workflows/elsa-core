using System;
using Elsa.Common.Services;
using Elsa.Scheduling.Activities;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Services;

namespace Elsa.Samples.Web1.Workflows;

public class HeartbeatWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
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