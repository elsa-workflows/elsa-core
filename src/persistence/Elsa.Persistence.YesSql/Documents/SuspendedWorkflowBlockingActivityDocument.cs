using NodaTime;

namespace Elsa.Persistence.YesSql.Documents
{
    public class SuspendedWorkflowBlockingActivityDocument : YesSqlDocument
    {
        
        public string EntityId { get; set; } = default!;

        public string InstanceId { get; set; } = default!;
        public string DefinitionId { get; set; } = default!;
        public int Version { get; set; }
        public string? CorrelationId { get; set; }
        public string? ContextId { get; set; }
        public string? TenantId { get; set; }
        public Instant CreatedAt { get; set; }
        public Instant? LastExecutedAt { get; set; }
        public string ActivityId { get; set; } = default!;
        public string ActivityType { get; set; } = default!;
    }
}