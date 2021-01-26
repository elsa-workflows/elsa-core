namespace Elsa.Models
{
    public class WorkflowTrigger : Entity, ITenantScope
    {
        public string? TenantId { get; set; }
        public string Hash { get; set; } = default!;
        public string ActivityType { get; set; } = default!;
        public string ActivityId { get; set; } = default!;
        public string WorkflowDefinitionId { get; set; } = default!;
        public string? WorkflowInstanceId { get; set; }
    }
}