namespace Elsa.MassTransit.Messages;

/// Represents a distributed message that multiple workflow definition versions have been deleted.
public class WorkflowDefinitionVersionsUpdated(IEnumerable<WorkflowDefinitionVersionUpdate> workflowDefinitionVersionUpdates)
{
    /// Represents a collection of updates to workflow definition versions.
    public IEnumerable<WorkflowDefinitionVersionUpdate> WorkflowDefinitionVersionUpdates { get; set; } = workflowDefinitionVersionUpdates;
}