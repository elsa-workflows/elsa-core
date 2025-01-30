namespace Elsa.Workflows.CommitStates;

public class DefaultCommitStrategyRegistry : ICommitStrategyRegistry
{
    private readonly IDictionary<string, IWorkflowCommitStrategy> _workflowStrategies = new Dictionary<string, IWorkflowCommitStrategy>();
    private readonly IDictionary<string, IActivityCommitStrategy> _activityStrategies = new Dictionary<string, IActivityCommitStrategy>();


    public IEnumerable<string> ListWorkflowStrategies()
    {
        return _workflowStrategies.Keys;
    }

    public IEnumerable<string> ListActivityStrategies()
    {
        return _activityStrategies.Keys;
    }

    public void RegisterStrategy(string name, IWorkflowCommitStrategy strategy)
    {
        _workflowStrategies[name] = strategy;
    }

    public void RegisterStrategy(string name, IActivityCommitStrategy strategy)
    {
        _activityStrategies[name] = strategy;
    }

    public IWorkflowCommitStrategy? FindWorkflowStrategy(string name)
    {
        return _workflowStrategies.TryGetValue(name, out var strategy) ? strategy : null;
    }

    public IActivityCommitStrategy? FindActivityStrategy(string name)
    {
        return _activityStrategies.TryGetValue(name, out var strategy) ? strategy : null;
    }
}