using Elsa.Workflows.Activities;

namespace Elsa.Workflows.IntegrationTests.Activities.Workflows;

public class BasicForkWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.Root = new Fork
        {
            Branches =
            {
                new WriteLine("Branch 1"),
                new WriteLine("Branch 2"),
                new WriteLine("Branch 3")
            }
        };
    }
}
