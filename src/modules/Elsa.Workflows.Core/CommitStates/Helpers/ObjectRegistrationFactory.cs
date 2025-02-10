namespace Elsa.Workflows.CommitStates;

public static class ObjectRegistrationFactory
{
    public static WorkflowCommitStrategyRegistration Describe(IWorkflowCommitStrategy strategy)
    {
        var metadata = ObjectMetadataDescriber.Describe(strategy.GetType());
        return new(strategy, metadata);
    }
    
    public static ActivityCommitStrategyRegistration Describe(IActivityCommitStrategy strategy)
    {
        var metadata = ObjectMetadataDescriber.Describe(strategy.GetType());
        return new(strategy, metadata);
    }
}