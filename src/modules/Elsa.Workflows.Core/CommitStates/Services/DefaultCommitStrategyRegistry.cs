namespace Elsa.Workflows.CommitStates;

public class DefaultCommitStrategyRegistry : ICommitStrategyRegistry
{
    private readonly IDictionary<string, WorkflowCommitStrategyRegistration> _workflowStrategies = new Dictionary<string, WorkflowCommitStrategyRegistration>();
    private readonly IDictionary<string, ActivityCommitStrategyRegistration> _activityStrategies = new Dictionary<string, ActivityCommitStrategyRegistration>();


    public IEnumerable<WorkflowCommitStrategyRegistration> ListWorkflowStrategyRegistrations()
    {
        return _workflowStrategies.Values;
    }

    public IEnumerable<ActivityCommitStrategyRegistration> ListActivityStrategyRegistrations()
    {
        return _activityStrategies.Values;
    }

    public void RegisterStrategy(WorkflowCommitStrategyRegistration registration)
    {
        _workflowStrategies[registration.Metadata.Name] = registration;
    }

    public void RegisterStrategy(ActivityCommitStrategyRegistration registration)
    {
        _activityStrategies[registration.Metadata.Name] = registration;
    }

    public IWorkflowCommitStrategy? FindWorkflowStrategy(string name)
    {
        return _workflowStrategies.TryGetValue(name, out var registration) ? registration.Strategy : null;
    }

    public IActivityCommitStrategy? FindActivityStrategy(string name)
    {
        return _activityStrategies.TryGetValue(name, out var registration) ? registration.Strategy : null;
    }
}