using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Services;

namespace Elsa.IntegrationTests.Activities;

class SequentialWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowDefinitionBuilder workflow)
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