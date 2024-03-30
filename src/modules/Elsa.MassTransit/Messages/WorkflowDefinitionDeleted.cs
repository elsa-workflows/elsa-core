namespace Elsa.MassTransit.Messages;

/// <summary>
/// Represents a distributed message that is triggered when a workflow definition is deleted.
/// </summary>
public class WorkflowDefinitionDeleted(string id)
{
    public string Id { get; set; } = id;
}