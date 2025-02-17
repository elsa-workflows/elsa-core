using System.ComponentModel;

namespace Elsa.Workflows.CommitStates.Strategies;

/// <summary>
/// Represents a strategy that always commits the workflow whenever the associated activity is about to execute or has executed.
/// This strategy ensures that the workflow's state is persisted at both pre-execution and post-execution stages of the activity.
/// </summary>
[DisplayName("Always Commit")]
[Description("Always commit the workflow state when the activity is about to execute or has executed.")]
public class CommitAlwaysActivityStrategy : IActivityCommitStrategy
{
    public CommitAction ShouldCommit(ActivityCommitStateStrategyContext context)
    {
        return CommitAction.Commit;
    }
}