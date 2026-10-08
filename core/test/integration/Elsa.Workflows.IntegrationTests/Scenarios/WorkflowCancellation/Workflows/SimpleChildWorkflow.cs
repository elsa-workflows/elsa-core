using Elsa.Workflows.Activities;

namespace Elsa.Workflows.IntegrationTests.Scenarios.WorkflowCancellation.Workflows;

public class SimpleChildWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Sequence
        {
            Activities =
            {
                new WriteLine("Executed SimpleChildWorkflow")
            }
        };
    }
}