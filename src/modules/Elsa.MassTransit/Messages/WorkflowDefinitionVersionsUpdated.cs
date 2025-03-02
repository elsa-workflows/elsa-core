namespace Elsa.MassTransit.Messages;

/// <summary>
/// Represents a distributed message that multiple workflow definition versions have been deleted.
/// </summary>
public class WorkflowDefinitionVersionsUpdated(IEnumerable<WorkflowDefinitionVersionUpdate> workflowDefinitionVersionUpdates)
{
    /// <summary>
    /// Represents a collection of updates to workflow definition versions.
    /// </summary>
    public IEnumerable<WorkflowDefinitionVersionUpdate> WorkflowDefinitionVersionUpdates { get; set; } = workflowDefinitionVersionUpdates;
}