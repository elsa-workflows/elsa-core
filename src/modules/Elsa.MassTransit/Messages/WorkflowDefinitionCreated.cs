namespace Elsa.MassTransit.Messages;

/// <summary>
/// Represents a distributed message that a workflow definition has been created.
/// </summary>
public class WorkflowDefinitionCreated(string id, bool usableAsActivity)
{
    /// <summary>
    /// The ID of the created workflow definition.
    /// </summary>
    public string Id { get; } = id;
    
    /// <summary>
    /// Whether the created workflow definition is usable as an activity.
    /// </summary>
    public bool UsableAsActivity { get; } = usableAsActivity;
}