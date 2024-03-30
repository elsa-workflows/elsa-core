namespace Elsa.MassTransit.Messages;

/// <summary>
/// Represents a distributed message that a workflow definition has been created.
/// </summary>
public class WorkflowDefinitionCreated(string id)
{
    public string Id { get; set; } = id;
}