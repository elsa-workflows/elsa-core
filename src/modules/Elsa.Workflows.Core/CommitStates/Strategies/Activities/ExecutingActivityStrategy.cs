namespace Elsa.Workflows.CommitStates.Strategies.Activities;

public class ExecutingActivityStrategy : IActivityCommitStrategy
{
    public CommitAction ShouldCommit(ActivityCommitStateStrategyContext context)
    {
        return context.LifetimeEvent == ActivityLifetimeEvent.ActivityExecuting ? CommitAction.Commit : CommitAction.Default;
    }
}