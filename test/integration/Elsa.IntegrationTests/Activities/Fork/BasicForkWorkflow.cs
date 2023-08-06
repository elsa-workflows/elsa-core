using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.IntegrationTests.Activities;

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