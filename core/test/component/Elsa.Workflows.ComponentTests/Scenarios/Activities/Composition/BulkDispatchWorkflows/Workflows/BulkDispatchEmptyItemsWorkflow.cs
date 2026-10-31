using JetBrains.Annotations;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Composition.BulkDispatchWorkflows.Workflows;

[UsedImplicitly]
public class BulkDispatchEmptyItemsWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = nameof(BulkDispatchEmptyItemsWorkflow);

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);

        builder.Root = new Runtime.Activities.BulkDispatchWorkflows
        {
            WorkflowDefinitionId = new(BulkChildWorkflow.DefinitionId),
            Items = new(Array.Empty<object>()),
            WaitForCompletion = new(true)
        };
    }
}
