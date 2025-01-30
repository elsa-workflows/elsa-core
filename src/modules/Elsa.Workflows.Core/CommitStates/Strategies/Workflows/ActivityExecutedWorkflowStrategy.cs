using System.ComponentModel;

namespace Elsa.Workflows.CommitStates.Strategies;

/// <summary>
/// Represents a commit strategy that determines whether a workflow state should be committed
/// based on the "ActivityExecuted" lifetime event of an activity.
/// </summary>
/// <remarks>
/// This strategy evaluates the <see cref="WorkflowCommitStateStrategyContext"/> provided during execution
/// and returns a <see cref="CommitAction.Commit"/> action if the activity's lifetime event corresponds
/// to <see cref="WorkflowLifetimeEvent.ActivityExecuted"/>. Otherwise, it defaults to <see cref="CommitAction.Default"/>.
/// </remarks>
[DisplayName("Activity Executed")]
[Description("Determines whether a workflow state should be committed if the current activity has executed.")]
public class ActivityExecutedWorkflowStrategy : IWorkflowCommitStrategy
{
    public CommitAction ShouldCommit(WorkflowCommitStateStrategyContext context)
    {
        return context.LifetimeEvent == WorkflowLifetimeEvent.ActivityExecuted ? CommitAction.Commit : CommitAction.Default;
    }
}