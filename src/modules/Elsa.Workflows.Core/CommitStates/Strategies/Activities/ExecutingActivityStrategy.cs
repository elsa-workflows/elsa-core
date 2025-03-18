using System.ComponentModel;

namespace Elsa.Workflows.CommitStates.Strategies;

/// <summary>
/// Represents a commit strategy that evaluates whether a workflow commit should occur
/// when an activity is in the "Executing" state.
/// </summary>
/// <remarks>
/// This strategy determines commit behavior based on the activity's lifecycle event.
/// Specifically, it commits the workflow if the activity is currently executing
/// (i.e., the lifetime event is `ActivityLifetimeEvent.ActivityExecuting`).
/// For all other states, it defaults to no specific commit action.
/// </remarks>
[DisplayName("Executing")]
[Description("Commit the workflow state when the activity is executing.")]
public class ExecutingActivityStrategy : IActivityCommitStrategy
{
    public CommitAction ShouldCommit(ActivityCommitStateStrategyContext context)
    {
        return context.LifetimeEvent == ActivityLifetimeEvent.ActivityExecuting ? CommitAction.Commit : CommitAction.Default;
    }
}