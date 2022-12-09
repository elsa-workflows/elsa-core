using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Services;
using Pipelines.Sockets.Unofficial;

namespace Elsa.IntegrationTests.Activities;

public class FinishForkedWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.WithRoot(new Fork
        {
            JoinMode = ForkJoinMode.WaitAll,
            Branches =
            {
                new Sequence
                {
                    Activities =
                    {
                        new WriteLine("Branch 1"),
                        Event.FromName("Event 1")
                    }
                },
                new Sequence
                {
                    Activities =
                    {
                        new WriteLine("Branch 2"),
                        new Finish()
                    }
                }
            }
        });
    }
}