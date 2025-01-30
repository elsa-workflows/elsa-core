namespace Elsa.Workflows.CommitStates.Strategies.Workflows;

public class ActivityExecutingWorkflowStrategy : IWorkflowCommitStrategy
{
    public CommitAction ShouldCommit(WorkflowCommitStateStrategyContext context)
    {
        return context.LifetimeEvent == WorkflowLifetimeEvent.ActivityExecuting ? CommitAction.Commit : CommitAction.Default;
    }
}