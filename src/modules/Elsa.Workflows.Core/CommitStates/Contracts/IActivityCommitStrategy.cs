namespace Elsa.Workflows.CommitStates;

public interface IActivityCommitStrategy
{
    CommitAction ShouldCommit(ActivityCommitStateStrategyContext context);
}