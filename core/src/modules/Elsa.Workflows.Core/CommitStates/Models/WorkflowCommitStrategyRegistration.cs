namespace Elsa.Workflows.CommitStates;

public class WorkflowCommitStrategyRegistration : ObjectRegistration<IWorkflowCommitStrategy, CommitStrategyMetadata>
{
    public WorkflowCommitStrategyRegistration()
    {
    }

    public WorkflowCommitStrategyRegistration(IWorkflowCommitStrategy strategy, CommitStrategyMetadata metadata)
    {
        Strategy = strategy;
        Metadata = metadata;
    }
}