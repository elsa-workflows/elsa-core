using Elsa.Workflows.Runtime.Models;

namespace Elsa.MassTransit.Messages;

/// Represents a message that indicates that the specified workflow definitions have been reloaded.
public class WorkflowDefinitionsReloaded(ICollection<ReloadedWorkflowDefinition> reloadedWorkflowDefinitions)
{
    /// The reloaded workflow definitions.  
    public ICollection<ReloadedWorkflowDefinition> ReloadedWorkflowDefinitions { get; set; } = reloadedWorkflowDefinitions;
}