using Elsa.Workflows.Activities;
using Elsa.Workflows.Runtime.Activities;

namespace Elsa.Workflows.IntegrationTests.Scenarios.BulkDispatchWithInput;

public class ParentWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new BulkDispatchWorkflows
        {
            WorkflowDefinitionId = new(nameof(ChildWorkflow)),
            Items = new(new[] { "Apple", "Banana", "Cherry" }),
            Input = new(new Dictionary<string, object> { ["ExtraData"] = "SharedValue" }),
            WaitForCompletion = new(false)
        };
    }
}

public class ChildWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new WriteLine("Child executed");
    }
}
