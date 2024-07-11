namespace Elsa.MassTransit.Messages;

/// <summary>
/// Represents a distributed message that a workflow definition version has been deleted.
/// </summary>
public class WorkflowDefinitionVersionDeleted(string id)
{
    public string Id { get; set; } = id;
}