using Elsa.Workflows.Activities;
using Elsa.Workflows.Runtime.Activities;

namespace Elsa.Workflows.IntegrationTests.Scenarios.WorkflowCancellation.Workflows;

public class SimpleSuspendedWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Sequence
        {
            Activities =
            {
                new Start(),
                new Event("BlockingEvent"),
                new WriteLine("Workflow was not properly blocked")
            },
        };
    }
}