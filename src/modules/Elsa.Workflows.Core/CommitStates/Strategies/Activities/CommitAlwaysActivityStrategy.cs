namespace Elsa.Workflows.CommitStates.Strategies.Activities;

public class CommitAlwaysActivityStrategy : IActivityCommitStrategy
{
    public CommitAction ShouldCommit(ActivityCommitStateStrategyContext context)
    {
        return CommitAction.Commit;
    }
}