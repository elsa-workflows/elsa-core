namespace Elsa.Workflows.CommitStates;

public interface ICommitStrategyRegistry
{
    IEnumerable<WorkflowCommitStrategyRegistration> ListWorkflowStrategyRegistrations();
    IEnumerable<ActivityCommitStrategyRegistration> ListActivityStrategyRegistrations();
    void RegisterStrategy(WorkflowCommitStrategyRegistration registration);
    void RegisterStrategy(ActivityCommitStrategyRegistration registration);
    IWorkflowCommitStrategy? FindWorkflowStrategy(string name);
    IActivityCommitStrategy? FindActivityStrategy(string name);
}