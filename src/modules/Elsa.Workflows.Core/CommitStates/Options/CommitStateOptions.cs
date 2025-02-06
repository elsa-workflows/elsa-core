namespace Elsa.Workflows.CommitStates;

public class CommitStateOptions
{
    public IDictionary<string, WorkflowCommitStrategyRegistration> WorkflowCommitStrategies { get; set; } = new Dictionary<string, WorkflowCommitStrategyRegistration>();
    public IDictionary<string, ActivityCommitStrategyRegistration> ActivityCommitStrategies { get; set; } = new Dictionary<string, ActivityCommitStrategyRegistration>();
}