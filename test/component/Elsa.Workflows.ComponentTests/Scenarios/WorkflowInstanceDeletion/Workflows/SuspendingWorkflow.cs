using Elsa.Workflows.Activities;
using Elsa.Workflows.ComponentTests.Scenarios.WorkflowInstanceDeletion.Activities;

namespace Elsa.Workflows.ComponentTests.Scenarios.WorkflowInstanceDeletion.Workflows;

/// <summary>
/// A workflow that uses a long-running activity to test deletion during active execution.
/// This simulates the race condition where a workflow instance is deleted while still executing in memory.
/// </summary>
public class SuspendingWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = "suspending-workflow-test";

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);

        builder.Root = new Sequence
        {
            Activities =
            {
                new WriteLine("Starting workflow"),
                new LongRunningActivity { DelayMilliseconds = 3000 }, // Runs for 3 seconds in memory
                new WriteLine("Workflow completed")
            }
        };
    }
}
