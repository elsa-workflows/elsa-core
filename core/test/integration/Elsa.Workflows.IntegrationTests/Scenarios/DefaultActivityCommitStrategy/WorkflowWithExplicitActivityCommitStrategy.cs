using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.IntegrationTests.Scenarios.DefaultActivityCommitStrategy;

public class WorkflowWithExplicitActivityCommitStrategy : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        var writeLineWithStrategy = new WriteLine("Activity with strategy");
        writeLineWithStrategy.CommitStrategy = "ExecutingActivity"; // Uses standard "Commit Before" strategy

        builder.Root = new Sequence
        {
            Activities =
            {
                writeLineWithStrategy,
                new WriteLine("Activity 2")
            }
        };
    }
}
