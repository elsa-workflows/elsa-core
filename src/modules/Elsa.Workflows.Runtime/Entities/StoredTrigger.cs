using Elsa.Common.Entities;

namespace Elsa.Workflows.Runtime.Entities;

/// <summary>
/// Represents a trigger associated with a workflow definition.
/// </summary>
public class StoredTrigger : Entity
{
    public string WorkflowDefinitionId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string ActivityId { get; set; } = default!;
    public string? Hash { get; set; }
    public object? Payload { get; set; }
}