using Elsa.Persistence.Common.Entities;

namespace Elsa.Workflows.Persistence.Entities;

/// <summary>
/// Represents a trigger associated with a workflow definition.
/// </summary>
public class WorkflowTrigger : Entity
{
    public string WorkflowDefinitionId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Hash { get; set; }
    public string? Data { get; set; }
}