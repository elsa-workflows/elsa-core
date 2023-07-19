using Elsa.Common.Entities;

namespace Elsa.Workflows.Runtime.Entities;

/// <summary>
/// Represents a trigger associated with a workflow definition.
/// </summary>
public class StoredTrigger : Entity
{
    /// <summary>
    /// The ID of the workflow definition.
    /// </summary>
    public string WorkflowDefinitionId { get; set; } = default!;
    
    /// <summary>
    /// The version ID of the workflow definition.
    /// </summary>
    public string WorkflowDefinitionVersionId { get; set; } = default!;
    
    /// <summary>
    /// The name of the trigger.
    /// </summary>
    public string Name { get; set; } = default!;
    
    /// <summary>
    /// The ID of the activity associated with the trigger.
    /// </summary>
    public string ActivityId { get; set; } = default!;
    
    /// <summary>
    /// The hash of the trigger.
    /// </summary>
    public string? Hash { get; set; }
    
    /// <summary>
    /// The payload of the trigger.
    /// </summary>
    public object? Payload { get; set; }
}