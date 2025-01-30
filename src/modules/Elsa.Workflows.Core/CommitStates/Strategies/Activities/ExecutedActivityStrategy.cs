namespace Elsa.Workflows.CommitStates.Strategies.Activities;

public class ExecutedActivityStrategy : IActivityCommitStrategy
{
    public CommitAction ShouldCommit(ActivityCommitStateStrategyContext context)
    {
        return context.LifetimeEvent == ActivityLifetimeEvent.ActivityExecuted ? CommitAction.Commit : CommitAction.Default;
    }
}