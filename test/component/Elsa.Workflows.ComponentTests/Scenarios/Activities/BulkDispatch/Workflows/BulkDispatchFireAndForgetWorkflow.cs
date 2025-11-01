using Elsa.Workflows.Runtime.Activities;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.BulkDispatch.Workflows;

public class BulkDispatchFireAndForgetWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString();

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);
        builder.Root = new BulkDispatchWorkflows
        {
            WorkflowDefinitionId = new(SlowBulkChildWorkflow.DefinitionId),
            Items = new(new object[] { "A", "B", "C" }),
            WaitForCompletion = new(false)
        };
    }
}
