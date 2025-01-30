namespace Elsa.Workflows.CommitStates.Options;

public class CommitStateOptions
{
    public IDictionary<string, IWorkflowCommitStrategy> WorkflowCommitStrategies { get; set; } = new Dictionary<string, IWorkflowCommitStrategy>();
    public IDictionary<string, IActivityCommitStrategy> ActivityCommitStrategies { get; set; } = new Dictionary<string, IActivityCommitStrategy>();
}