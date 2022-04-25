using Elsa.Activities;
using Elsa.Modules.Activities.Console;
using Elsa.Services;

namespace Elsa.IntegrationTests.Activities;

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