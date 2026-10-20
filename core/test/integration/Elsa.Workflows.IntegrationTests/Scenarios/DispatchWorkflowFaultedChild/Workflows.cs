using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.Runtime.Activities;

namespace Elsa.Workflows.IntegrationTests.Scenarios.DispatchWorkflowFaultedChild;

/// <summary>A child workflow that faults as soon as it runs.</summary>
public class FaultingChildWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder) =>
        builder.Root = Fault.Create("CHILD-01", "Test", "Business", "The child failed.");
}

/// <summary>A flowchart that dispatches a <see cref="FaultingChildWorkflow"/>, waits for it, and continues on <c>Done</c>.</summary>
public class DispatchFaultingChildWorkflow : WorkflowBase
{
    public const string ParentCompletedText = "Parent completed";

    protected override void Build(IWorkflowBuilder builder)
    {
        var dispatch = new DispatchWorkflow
        {
            WorkflowDefinitionId = new(nameof(FaultingChildWorkflow)),
            WaitForCompletion = new(true)
        };
        var parentCompleted = new WriteLine(ParentCompletedText);

        builder.Root = new Flowchart
        {
            Activities = { dispatch, parentCompleted },
            Connections = { new Connection(dispatch, parentCompleted) }
        };
    }
}
