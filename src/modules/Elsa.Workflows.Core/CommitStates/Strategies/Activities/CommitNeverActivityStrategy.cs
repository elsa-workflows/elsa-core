namespace Elsa.Workflows.CommitStates.Strategies.Activities;

public class CommitNeverActivityStrategy : IActivityCommitStrategy
{
    public CommitAction ShouldCommit(ActivityCommitStateStrategyContext context)
    {
        return CommitAction.Skip;
    }
}