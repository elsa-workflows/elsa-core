using System.Collections.Generic;
using Elsa.Comparers;
using Newtonsoft.Json.Linq;
using NodaTime;

namespace Elsa.Models
{
    public class WorkflowInstance : Entity, ITenantScope, ICorrelationScope
    {
        private HashSet<BlockingActivity> _blockingActivities = new(BlockingActivityEqualityComparer.Instance);

        public WorkflowInstance()
        {
            Variables = new Variables();
            ScheduledActivities = new SimpleStack<ScheduledActivity>();
            Scopes = new SimpleStack<ActivityScope>();
        }

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
        public Variables Variables { get; set; }
        public object? Output { get; set; }
        public IDictionary<string, JObject> ActivityData { get; set; } = new Dictionary<string, JObject>();
        public IDictionary<string, object> ActivityOutput { get; set; } = new Dictionary<string, object>();

        public HashSet<BlockingActivity> BlockingActivities
        {
            get => _blockingActivities;
            set => _blockingActivities = new HashSet<BlockingActivity>(value, BlockingActivityEqualityComparer.Instance);
        }

        public WorkflowFault? Fault { get; set; }
        public SimpleStack<ScheduledActivity> ScheduledActivities { get; set; }
        public SimpleStack<ActivityScope> Scopes { get; set; }
        public ScheduledActivity? CurrentActivity { get; set; }
    }
}