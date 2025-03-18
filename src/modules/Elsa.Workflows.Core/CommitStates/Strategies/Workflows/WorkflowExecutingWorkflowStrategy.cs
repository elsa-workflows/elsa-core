using System.ComponentModel;

namespace Elsa.Workflows.CommitStates.Strategies;

/// <summary>
/// Implements a commit strategy that determines whether the workflow state
/// should be committed when the workflow is in the "executing" lifetime event.
/// </summary>
/// <remarks>
/// This strategy checks the current context's `LifetimeEvent` and commits
/// the workflow state if it corresponds to the `WorkflowExecuting` event.
/// Otherwise, it defaults to no explicit commit action.
/// </remarks>
[DisplayName("Workflow Executing")]
[Description("Commit the workflow state when the workflow is executing.")]
public class WorkflowExecutingWorkflowStrategy : IWorkflowCommitStrategy
{
    public CommitAction ShouldCommit(WorkflowCommitStateStrategyContext context)
    {
        return context.LifetimeEvent == WorkflowLifetimeEvent.WorkflowExecuting ? CommitAction.Commit : CommitAction.Default;
    }
}