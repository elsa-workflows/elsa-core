namespace Elsa.MassTransit.Messages;

/// Represents a message that indicates that the specified workflow definitions have been reloaded.
public class WorkflowDefinitionsReloaded(ICollection<string> workflowDefinitionIds)
{
    /// The workflow definition IDs that have been reloaded.  
    public ICollection<string> WorkflowDefinitionIds { get; set; } = workflowDefinitionIds;
}