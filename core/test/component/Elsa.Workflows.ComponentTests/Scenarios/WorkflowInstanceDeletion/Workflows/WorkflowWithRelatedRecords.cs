using Elsa.Scheduling.Activities;
using Elsa.Workflows.Activities;

namespace Elsa.Workflows.ComponentTests.Scenarios.WorkflowInstanceDeletion.Workflows;

/// <summary>
/// A workflow that generates related records: execution logs, activity execution records, and bookmarks.
/// This is used to test that all related records are properly deleted when the workflow instance is deleted.
/// </summary>
public class WorkflowWithRelatedRecords : WorkflowBase
{
    public static readonly string DefinitionId = "workflow-with-related-records-test";

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);

        builder.Root = new Sequence
        {
            Activities =
            {
                // This will create activity execution records and execution logs
                new WriteLine("Step 1: Starting workflow"),
                new WriteLine("Step 2: About to delay"),

                // This will create a bookmark (suspends the workflow)
                new Delay(TimeSpan.FromHours(1)),

                new WriteLine("Step 3: This should never execute")
            }
        };
    }
}
