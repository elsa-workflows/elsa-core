using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Services;

namespace Elsa.IntegrationTests.Activities;

public class BasicForkWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowDefinitionBuilder workflow)
    {
        workflow.WithRoot(new Fork
        {
            Branches =
            {
                new WriteLine("Branch 1"),
                new WriteLine("Branch 2"),
                new WriteLine("Branch 3")
            }
        });
    }
}