using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Timer = Elsa.Scheduling.Activities.Timer;

namespace Elsa.WorkflowServer.Web;

public class HeartbeatWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Sequence
        {
            Activities =
            {
                new Timer(TimeSpan.FromMinutes(5))
                {
                    CanStartWorkflow = true
                },
                new WriteLine(context => $"Heartbeat workflow triggered at {DateTime.Now}")
            }
        };
    }
}