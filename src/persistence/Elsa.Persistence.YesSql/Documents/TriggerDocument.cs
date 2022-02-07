namespace Elsa.Persistence.YesSql.Documents;

public class TriggerDocument : YesSqlDocument
{
    public string TriggerId { get; set; } = default!;
    public string? TenantId { get; set; }
    public string Hash { get; set; } = default!;
    public string Model { get; set; } = default!;
    public string ModelType { get; set; } = default!;
    public string ActivityType { get; set; } = default!;
    public string ActivityId { get; set; } = default!;
    public string WorkflowDefinitionId { get; set; } = default!;
}