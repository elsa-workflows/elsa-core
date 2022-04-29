using Elsa.Activities;
using Elsa.Modules.Activities.Console;
using Elsa.Services;

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