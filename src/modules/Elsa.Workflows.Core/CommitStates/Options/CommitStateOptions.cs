namespace Elsa.Workflows.CommitStates;

/// <summary>
/// Configuration options for commit state strategies.
/// </summary>
public class CommitStateOptions
{
    /// <summary>
    /// Gets or sets the workflow commit strategies.
    /// </summary>
    public IDictionary<string, WorkflowCommitStrategyRegistration> WorkflowCommitStrategies { get; set; } = new Dictionary<string, WorkflowCommitStrategyRegistration>();

    /// <summary>
    /// Gets or sets the activity commit strategies.
    /// </summary>
    public IDictionary<string, ActivityCommitStrategyRegistration> ActivityCommitStrategies { get; set; } = new Dictionary<string, ActivityCommitStrategyRegistration>();

    /// <summary>
    /// Gets or sets the default workflow commit strategy instance to use when a workflow does not specify its own.
    /// This strategy is not added to the registry and serves only as a fallback.
    /// </summary>
    public IWorkflowCommitStrategy? DefaultWorkflowCommitStrategy { get; set; }

    /// <summary>
    /// Gets or sets the default activity commit strategy instance to use when an activity does not specify its own.
    /// This strategy is not added to the registry and serves only as a fallback.
    /// </summary>
    public IActivityCommitStrategy? DefaultActivityCommitStrategy { get; set; }
}