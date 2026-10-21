using Elsa.Workflows.IntegrationTests.Scenarios.DispatchWorkflowCancellation;
using Elsa.Workflows.Runtime.Activities;

namespace Elsa.Workflows.IntegrationTests.Scenarios.ExecuteWorkflowCancellation;

/// <summary>A parent that executes <see cref="BlockingChildWorkflow"/>, waits for it, and finishes once it is resumed.</summary>
public class ExecuteAndWaitWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new ExecuteWorkflow
        {
            WorkflowDefinitionId = new(nameof(BlockingChildWorkflow)),
            WaitForCompletion = new(true)
        };
    }
}
