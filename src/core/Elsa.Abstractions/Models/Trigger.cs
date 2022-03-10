namespace Elsa.Models;

public class Trigger : Entity, ITenantScope
{
    public string? TenantId { get; set; }
    public string Hash { get; set; } = default!;
    public string Model { get; set; } = default!;
    public string ModelType { get; set; } = default!;
    public string ActivityType { get; set; } = default!;
    public string ActivityId { get; set; } = default!;
    public string WorkflowDefinitionId { get; set; } = default!;
}