using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.IntegrationTests.Activities;

public class FinishSequentialWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.Root = new Sequence
        {
            Activities =
            {
                new WriteLine("Line 1"),
                new Finish(),
                new WriteLine("Line 2")
            }
        };
    }
}