namespace Elsa.MassTransit.Messages;

public class WorkflowDefinitionPublished(string id, bool usableAsActivity)
{
    public string Id { get; set; } = id;
    public bool UsableAsActivity { get; set; } = usableAsActivity;
}