using Elsa.Activities;
using Elsa.Services;

namespace Elsa.IntegrationTests.Activities;

public class FinishForkedWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowDefinitionBuilder workflow)
    {
        workflow.WithRoot(new Fork
        {
            JoinMode = JoinMode.WaitAll,
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
        });
    }
}