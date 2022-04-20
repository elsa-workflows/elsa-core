using Elsa.Activities;
using Elsa.Contracts;
using Elsa.Modules.Activities.Console;

namespace Elsa.IntegrationTests.Activities;

public class BasicForkWorkflow : IWorkflow
{
    public void Build(IWorkflowDefinitionBuilder workflow)
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