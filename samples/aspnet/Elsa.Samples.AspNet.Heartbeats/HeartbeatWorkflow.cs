using Elsa.Common;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Timer = Elsa.Scheduling.Activities.Timer;

namespace Elsa.Samples.AspNet.Heartbeats;

public class HeartbeatWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Sequence
        {
            Activities =
            {
                new Timer(TimeSpan.FromSeconds(5))
                {
                    CanStartWorkflow = true
                },
                new WriteLine(context => $"Heartbeat at {context.GetRequiredService<ISystemClock>().UtcNow}"),
            }
        };
    }
}