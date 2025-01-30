namespace Elsa.Workflows.CommitStates;

public interface ICommitStrategyRegistry
{
    IEnumerable<string> ListWorkflowStrategies();
    IEnumerable<string> ListActivityStrategies();
    void RegisterStrategy(string name, IWorkflowCommitStrategy strategy);
    void RegisterStrategy(string name, IActivityCommitStrategy strategy);
    IWorkflowCommitStrategy? FindWorkflowStrategy(string name);
    IActivityCommitStrategy? FindActivityStrategy(string name);
}