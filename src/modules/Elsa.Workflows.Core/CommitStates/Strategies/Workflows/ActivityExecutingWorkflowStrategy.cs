using System.ComponentModel;

namespace Elsa.Workflows.CommitStates.Strategies;

/// <summary>
/// Represents a workflow commit strategy that determines whether a commit should occur
/// during the ActivityExecuting lifecycle event of an activity.
/// </summary>
/// <remarks>
/// This strategy evaluates the workflow execution context and checks if the current lifecycle event is
/// <see cref="WorkflowLifetimeEvent.ActivityExecuting"/>. If the condition is met, the strategy indicates
/// that a commit action should be performed. Otherwise, a default action will be returned.
/// </remarks>
[DisplayName("Activity Executing")]
[Description("Determines whether a workflow state should be committed if the current activity is executing.")]
public class ActivityExecutingWorkflowStrategy : IWorkflowCommitStrategy
{
    public CommitAction ShouldCommit(WorkflowCommitStateStrategyContext context)
    {
        return context.LifetimeEvent == WorkflowLifetimeEvent.ActivityExecuting ? CommitAction.Commit : CommitAction.Default;
    }
}