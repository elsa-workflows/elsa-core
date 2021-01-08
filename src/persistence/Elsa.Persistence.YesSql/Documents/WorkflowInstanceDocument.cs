using System.Collections.Generic;
using Elsa.Comparers;
using Elsa.Models;
using Newtonsoft.Json.Linq;
using NodaTime;

namespace Elsa.Persistence.YesSql.Documents
{
    public class WorkflowInstanceDocument : YesSqlDocument
    {
        private HashSet<BlockingActivity> _blockingActivities = new(BlockingActivityEqualityComparer.Instance);

        public string InstanceId { get; set; } = default!;
        public string DefinitionId { get; set; } = default!;
        public string? TenantId { get; set; }
        public int Version { get; set; }
        public WorkflowStatus WorkflowStatus { get; set; }
        public string? CorrelationId { get; set; }
        public string? ContextType { get; set; }
        public string? ContextId { get; set; }
        public string? Name { get; set; }
        public Instant CreatedAt { get; set; }
        public Instant? LastExecutedAt { get; set; }
        public Instant? FinishedAt { get; set; }
        public Instant? CancelledAt { get; set; }
        public Instant? FaultedAt { get; set; }
        public Variables Variables { get; set; } = new();
        public object? Output { get; set; }
        public IDictionary<string, JObject> ActivityData { get; set; } = new Dictionary<string, JObject>();
        public IDictionary<string, object> ActivityOutput { get; set; } = new Dictionary<string, object>();

        public HashSet<BlockingActivity> BlockingActivities
        {
            get => _blockingActivities;
            set => _blockingActivities = new HashSet<BlockingActivity>(value, BlockingActivityEqualityComparer.Instance);
        }
        
        public WorkflowFault? Fault { get; set; }
        public Stack<ScheduledActivity> ScheduledActivities { get; set; } = new();
    }
}