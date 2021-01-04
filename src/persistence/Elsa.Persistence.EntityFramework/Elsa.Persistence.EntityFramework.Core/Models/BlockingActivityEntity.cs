namespace Elsa.Persistence.EntityFramework.Core.Models
{
    public class BlockingActivityEntity
    {
        public string Id { get; set; } = default!;
        public string WorkflowInstanceId { get; set; } = default!;
        public string? TenantId { get; set; }
        public string ActivityId { get; set; } = default!;
        public string ActivityType { get; set; }= default!;
        public string? Tag { get; set; }
    }
}