namespace Elsa.Workflows.CommitStates.Strategies.Workflows;

public class ActivityExecutedWorkflowStrategy : IWorkflowCommitStrategy
{
    public CommitAction ShouldCommit(WorkflowCommitStateStrategyContext context)
    {
        return context.LifetimeEvent == WorkflowLifetimeEvent.ActivityExecuted ? CommitAction.Commit : CommitAction.Default;
    }
}