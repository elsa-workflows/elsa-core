using Elsa.Workflows.Runtime.Models;

namespace Elsa.ServiceBus.MassTransit.Messages;

/// <summary>
/// Represents a message that indicates that the specified workflow definitions have been reloaded.
/// </summary>
public class WorkflowDefinitionsReloaded(ICollection<ReloadedWorkflowDefinition> reloadedWorkflowDefinitions)
{
    /// <summary>
    /// The reloaded workflow definitions.
    /// </summary>
    public ICollection<ReloadedWorkflowDefinition> ReloadedWorkflowDefinitions { get; set; } = reloadedWorkflowDefinitions;
}