using Elsa.Workflows.Activities;

namespace Elsa.Workflows.IntegrationTests.Activities.Workflows;

public class EmptyForkWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.Root = new Sequence
        {
            Activities =
            {
                new WriteLine("Before fork"),
                new Fork(),
                new WriteLine("After fork")
            }
        };
    }
}
