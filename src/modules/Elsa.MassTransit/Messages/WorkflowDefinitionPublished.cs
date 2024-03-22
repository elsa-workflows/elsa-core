namespace Elsa.MassTransit.Messages;

public class WorkflowDefinitionPublished(string id)
{
    public string Id { get; set; } = id;
}