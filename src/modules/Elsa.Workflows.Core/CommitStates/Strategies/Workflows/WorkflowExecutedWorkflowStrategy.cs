namespace Elsa.Workflows.CommitStates.Strategies.Workflows;

public class WorkflowExecutedWorkflowStrategy : IWorkflowCommitStrategy
{
    public CommitAction ShouldCommit(WorkflowCommitStateStrategyContext context)
    {
        return context.LifetimeEvent == WorkflowLifetimeEvent.WorkflowExecuted ? CommitAction.Commit : CommitAction.Default;
    }
}