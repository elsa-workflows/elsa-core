using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;

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