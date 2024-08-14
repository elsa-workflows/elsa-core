namespace Elsa.MassTransit.Messages;

/// Represents a message that indicates that the specified workflow definitions have been refreshed.
public class WorkflowDefinitionsRefreshed(ICollection<string> workflowDefinitionIds)
{
    /// The workflow definition IDs that have been refreshed.  
    public ICollection<string> WorkflowDefinitionIds { get; set; } = workflowDefinitionIds;
}