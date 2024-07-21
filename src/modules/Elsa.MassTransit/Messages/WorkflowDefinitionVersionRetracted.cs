namespace Elsa.MassTransit.Messages;

/// <summary>
/// Represents a distributed message that a workflow definition version has been retracted.
/// </summary>
public class WorkflowDefinitionVersionRetracted(string id, bool usableAsActivity)
{
    public string Id { get; set; } = id;
    public bool UsableAsActivity { get; set; } = usableAsActivity;
}