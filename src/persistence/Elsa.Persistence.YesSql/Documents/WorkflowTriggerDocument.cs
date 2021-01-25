namespace Elsa.Persistence.YesSql.Documents
{
    public class WorkflowTriggerDocument : YesSqlDocument
    {
        public string TriggerId { get; set; } = default!;
        public string? TenantId { get; set; }
        public string Data { get; set; } = default!;
        public string ActivityType { get; set; } = default!;
        public string WorkflowDefinitionId { get; set; } = default!;
        public string? WorkflowInstanceId { get; set; }
    }
}