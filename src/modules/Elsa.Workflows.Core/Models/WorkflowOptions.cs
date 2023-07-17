using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core.Models;

/// <summary>
/// Provides workflow options.
/// </summary>
public class WorkflowOptions
{
    /// <summary>
    /// The type of <see cref="IWorkflowActivationStrategy"/> to apply when new instances are requested to be created.
    /// </summary>
    public Type? ActivationStrategyType { get; set; }

    /// <summary>
    /// Used to decide if the workflow can be used as an activity.
    /// </summary>
    public bool? UsableAsActivity { get; set; }
    
    /// <summary>
    /// Used to decide if the consuming workflows should be updated automatically to use the last published version of the workflow when it is published.
    /// </summary>
    public bool AutoUpdateConsumingWorkflows { get; set; }
    
    /// <summary>
    /// The category to use when the workflow is used as an activity.
    /// </summary>
    public string? ActivityCategory { get; set; }
}