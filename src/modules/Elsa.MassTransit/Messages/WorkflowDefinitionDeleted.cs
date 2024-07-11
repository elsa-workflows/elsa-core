namespace Elsa.MassTransit.Messages;

/// <summary>
/// Represents a distributed message that is triggered when a workflow definition is deleted.
/// </summary>
public class WorkflowDefinitionDeleted(string id)
{
    /// <summary>
    /// The ID of the deleted workflow definition.
    /// </summary>
    public string Id { get; } = id;
}