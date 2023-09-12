namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

/// <summary>
/// Provides workflow options.
/// </summary>
public class WorkflowOptions
{
    /// <summary>
    /// The type of <c>IWorkflowActivationStrategy</c> to apply when new instances are requested to be created.
    /// </summary>
    public string? ActivationStrategyType { get; set; }

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

    /// <summary>
    /// The type of <c>IIncidentStrategy</c> to use when a fault occurs in the workflow.
    /// </summary>
    public string? IncidentStrategyType { get; set; }
}