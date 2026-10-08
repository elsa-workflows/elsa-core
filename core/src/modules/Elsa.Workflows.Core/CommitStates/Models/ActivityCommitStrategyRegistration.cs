namespace Elsa.Workflows.CommitStates;

public class ActivityCommitStrategyRegistration : ObjectRegistration<IActivityCommitStrategy, CommitStrategyMetadata>
{
    public ActivityCommitStrategyRegistration()
    {
    }
    
    public ActivityCommitStrategyRegistration(IActivityCommitStrategy strategy, CommitStrategyMetadata metadata)
    {
        Strategy = strategy;
        Metadata = metadata;
    }
}