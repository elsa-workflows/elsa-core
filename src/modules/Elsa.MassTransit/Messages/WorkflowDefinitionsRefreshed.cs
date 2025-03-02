namespace Elsa.MassTransit.Messages;

/// <summary>
/// Represents a message that indicates that the specified workflow definitions have been refreshed.
/// </summary>
public class WorkflowDefinitionsRefreshed(ICollection<string> workflowDefinitionIds)
{
    /// <summary>
    /// The workflow definition IDs that have been refreshed.
    /// </summary>
    public ICollection<string> WorkflowDefinitionIds { get; set; } = workflowDefinitionIds;
}