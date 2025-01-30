using System.ComponentModel;

namespace Elsa.Workflows.CommitStates.Strategies;

/// <summary>
/// A strategy implementation of <see cref="IActivityCommitStrategy"/> that specifies
/// the workflow should never commit when the associated activity is executed or has executed.
/// This overrides any workflow-level commit strategy.
/// </summary>
[DisplayName("Never Commit")]
[Description("Never commit the workflow state when the activity is about to execute or has executed.")]
public class CommitNeverActivityStrategy : IActivityCommitStrategy
{
    public CommitAction ShouldCommit(ActivityCommitStateStrategyContext context)
    {
        return CommitAction.Skip;
    }
}