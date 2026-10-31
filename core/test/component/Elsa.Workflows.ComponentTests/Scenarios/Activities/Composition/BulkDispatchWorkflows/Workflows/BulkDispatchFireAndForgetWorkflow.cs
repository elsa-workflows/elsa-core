namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Composition.BulkDispatchWorkflows.Workflows;

public class BulkDispatchFireAndForgetWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString();

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);
        builder.Root = new Runtime.Activities.BulkDispatchWorkflows
        {
            WorkflowDefinitionId = new(SlowBulkChildWorkflow.DefinitionId),
            Items = new(new object[] { "A", "B", "C" }),
            WaitForCompletion = new(false)
        };
    }
}
