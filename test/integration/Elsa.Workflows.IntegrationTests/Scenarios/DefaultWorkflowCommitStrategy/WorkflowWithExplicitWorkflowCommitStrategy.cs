using Elsa.Workflows.Activities;

namespace Elsa.Workflows.IntegrationTests.Scenarios.DefaultWorkflowCommitStrategy;

/// <summary>
/// A workflow that explicitly sets a commit strategy, which should override the default.
/// </summary>
public class WorkflowWithExplicitWorkflowCommitStrategy : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WorkflowOptions.CommitStrategyName = "WorkflowExecuting";

        builder.Root = new Sequence
        {
            Activities =
            {
                new WriteLine("Activity 1"),
                new WriteLine("Activity 2")
            }
        };
    }
}
