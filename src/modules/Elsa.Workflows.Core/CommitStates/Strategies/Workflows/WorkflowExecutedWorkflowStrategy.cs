using System.ComponentModel;

namespace Elsa.Workflows.CommitStates.Strategies;

/// <summary>
/// Represents a workflow commit strategy that commits changes when the workflow has been executed.
/// </summary>
[DisplayName("Workflow Executed")]
[Description("Commit the workflow state when the workflow has been executed.")]
public class WorkflowExecutedWorkflowStrategy : IWorkflowCommitStrategy
{
    public CommitAction ShouldCommit(WorkflowCommitStateStrategyContext context)
    {
        return context.LifetimeEvent == WorkflowLifetimeEvent.WorkflowExecuted ? CommitAction.Commit : CommitAction.Default;
    }
}