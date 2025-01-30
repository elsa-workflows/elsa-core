using System.ComponentModel;

namespace Elsa.Workflows.CommitStates.Strategies;

/// <summary>
/// Represents an activity commit strategy that determines whether a commit action should occur
/// after an activity has executed within a workflow.
/// </summary>
/// <remarks>
/// This strategy evaluates the activity's execution events, specifically committing only if the
/// activity's lifetime event is "ActivityExecuted".
/// </remarks>
[DisplayName("Executed")]
[Description("Commit the workflow state after the activity has executed.")]
public class ExecutedActivityStrategy : IActivityCommitStrategy
{
    public CommitAction ShouldCommit(ActivityCommitStateStrategyContext context)
    {
        return context.LifetimeEvent == ActivityLifetimeEvent.ActivityExecuted ? CommitAction.Commit : CommitAction.Default;
    }
}