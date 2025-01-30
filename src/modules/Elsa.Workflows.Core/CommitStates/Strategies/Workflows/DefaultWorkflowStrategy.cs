namespace Elsa.Workflows.CommitStates.Strategies.Workflows;

public class DefaultWorkflowStrategy : IWorkflowCommitStrategy
{
    public CommitAction ShouldCommit(WorkflowCommitStateStrategyContext context)
    {
        return CommitAction.Default;
    }
}