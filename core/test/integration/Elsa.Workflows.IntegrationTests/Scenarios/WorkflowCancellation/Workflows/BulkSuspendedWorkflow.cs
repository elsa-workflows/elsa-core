using Elsa.Scheduling.Activities;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Runtime.Activities;

namespace Elsa.Workflows.IntegrationTests.Scenarios.WorkflowCancellation.Workflows;

public class BulkSuspendedWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        object[] items = Enumerable.Range(0, 1500).Select(x => (object)x).ToArray();

        builder.Root = new Sequence
        {
            Activities =
            {
                new Start(),
                new Delay(TimeSpan.FromSeconds(10)),
                new BulkDispatchWorkflows
                {
                    WorkflowDefinitionId = new(nameof(SimpleChildWorkflow)),
                    Items = new(items)
                }
            },
        };
    }
}