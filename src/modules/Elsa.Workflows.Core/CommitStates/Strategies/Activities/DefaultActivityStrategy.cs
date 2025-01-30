namespace Elsa.Workflows.CommitStates.Strategies.Activities;

public class DefaultActivityStrategy : IActivityCommitStrategy
{
    public CommitAction ShouldCommit(ActivityCommitStateStrategyContext context)
    {
        return CommitAction.Default;
    }
}