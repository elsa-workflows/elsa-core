namespace Elsa.Workflows.CommitStates.Strategies.Workflows;

public class WorkflowExecutingWorkflowStrategy : IWorkflowCommitStrategy
{
    public CommitAction ShouldCommit(WorkflowCommitStateStrategyContext context)
    {
        return context.LifetimeEvent == WorkflowLifetimeEvent.WorkflowExecuting ? CommitAction.Commit : CommitAction.Default;
    }
}