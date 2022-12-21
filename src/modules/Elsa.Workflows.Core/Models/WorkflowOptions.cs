using Elsa.Workflows.Core.Services;

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
}