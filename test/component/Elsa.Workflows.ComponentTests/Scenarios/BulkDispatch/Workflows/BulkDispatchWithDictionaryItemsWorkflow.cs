using Elsa.Workflows.Runtime.Activities;

namespace Elsa.Workflows.ComponentTests.Scenarios.BulkDispatch.Workflows;

public class BulkDispatchWithDictionaryItemsWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString();

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);

        var items = new object[]
        {
            new Dictionary<string, object> { ["Name"] = "Alice", ["Age"] = 30 },
            new Dictionary<string, object> { ["Name"] = "Bob", ["Age"] = 25 },
            new Dictionary<string, object> { ["Name"] = "Charlie", ["Age"] = 35 }
        };

        builder.Root = new BulkDispatchWorkflows
        {
            WorkflowDefinitionId = new(BulkChildWorkflow.DefinitionId),
            Items = new(items),
            WaitForCompletion = new(true)
        };
    }
}
