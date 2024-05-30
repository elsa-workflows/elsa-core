namespace Elsa.MassTransit.Messages;

/// <summary>
/// Represents a distributed message that is triggered when a workflow definition is published.
/// </summary>
public class WorkflowDefinitionPublished(string id, bool usableAsActivity)
{
    /// <summary>
    /// The ID of the published workflow definition.
    /// </summary>
    public string Id { get; } = id;
    
    /// <summary>
    /// Whether the published workflow definition is usable as an activity.
    /// </summary>
    public bool UsableAsActivity { get; } = usableAsActivity;
}