using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Runtime.Activities;

namespace Elsa.IntegrationTests.Activities;

public class FinishForkedWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.Root = new Fork
        {
            JoinMode = ForkJoinMode.WaitAll,
            Branches =
            {
                new Sequence
                {
                    Activities =
                    {
                        new WriteLine("Branch 1"),
                        new Event("Event 1")
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
        };
    }
}