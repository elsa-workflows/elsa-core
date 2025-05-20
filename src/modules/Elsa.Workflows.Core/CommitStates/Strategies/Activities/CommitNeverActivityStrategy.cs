using System.ComponentModel;

namespace Elsa.Workflows.CommitStates.Strategies;

/// <summary>
/// A strategy implementation of <see cref="IActivityCommitStrategy"/> that specifies
/// the workflow should never commit when the associated activity is executed or has executed.
/// This overrides any workflow-level commit strategy.
/// </summary>
[DisplayName("Commit Later")]
[Description("Workflow state will not be commited before or after the activity executes.")]
public class CommitNeverActivityStrategy : IActivityCommitStrategy
{
    public CommitAction ShouldCommit(ActivityCommitStateStrategyContext context)
    {
        return CommitAction.Skip;
    }
}