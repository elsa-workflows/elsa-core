namespace Elsa.Models
{
    public class Bookmark : Entity, ITenantScope
    {
        public string? TenantId { get; set; }
        public string Hash { get; set; } = default!;
        public string Model { get; set; } = default!;
        public string ModelType { get; set; } = default!;
        public string ActivityType { get; set; } = default!;
        public string ActivityId { get; set; } = default!;
        public string WorkflowInstanceId { get; set; } = default!;
        public string CorrelationId { get; set; } = default!;
    }
}