namespace Elsa.Persistence.Entities;

public class WorkflowTrigger : Entity
{
    public string WorkflowDefinitionId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Hash { get; set; }
}