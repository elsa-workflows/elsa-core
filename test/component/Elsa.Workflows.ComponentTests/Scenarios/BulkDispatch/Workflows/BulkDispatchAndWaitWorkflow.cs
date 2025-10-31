using Elsa.Workflows.Activities;
using Elsa.Workflows.Runtime.Activities;

namespace Elsa.Workflows.ComponentTests.Scenarios.BulkDispatch.Workflows;

public class BulkDispatchAndWaitWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString();

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);
        builder.Root = new Sequence
        {
            Activities =
            {
                new BulkDispatchWorkflows
                {
                    WorkflowDefinitionId = new(BulkChildWorkflow.DefinitionId),
                    Items = new(new object[] { 1, 2, 3 }),
                    WaitForCompletion = new(true)
                },
                new WriteLine("Done")
            }
        };
    }
}
