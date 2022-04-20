using Elsa.Activities;
using Elsa.Contracts;
using Elsa.Modules.Activities.Activities.Console;

namespace Elsa.IntegrationTests.Workflows;

class SequentialWorkflow : IWorkflow
{
    public void Build(IWorkflowDefinitionBuilder workflow)
    {
        workflow.WithRoot(new Sequence
        {
            Activities =
            {
                new WriteLine("Line 1"),
                new WriteLine("Line 2"),
                new WriteLine("Line 3")
            }
        });
    }
}