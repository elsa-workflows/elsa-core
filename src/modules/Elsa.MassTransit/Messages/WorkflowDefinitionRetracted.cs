namespace Elsa.MassTransit.Messages;

/// <summary>
/// Represents a distributed message that a workflow definition has been retracted.
/// </summary>
public class WorkflowDefinitionRetracted(string id, bool usableAsActivity)
{
    public string Id { get; set; } = id;
    public bool UsableAsActivity { get; set; } = usableAsActivity;
}