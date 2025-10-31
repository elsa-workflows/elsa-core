using Elsa.Workflows.Runtime.Activities;
using JetBrains.Annotations;

namespace Elsa.Workflows.ComponentTests.Scenarios.BulkDispatch.Workflows;

[UsedImplicitly]
public class BulkDispatchEmptyItemsWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = nameof(BulkDispatchEmptyItemsWorkflow);

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);

        builder.Root = new BulkDispatchWorkflows
        {
            WorkflowDefinitionId = new(BulkChildWorkflow.DefinitionId),
            Items = new(Array.Empty<object>()),
            WaitForCompletion = new(true)
        };
    }
}
