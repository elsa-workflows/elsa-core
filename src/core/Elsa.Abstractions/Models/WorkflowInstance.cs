using System;
using System.Collections.Generic;
using System.Linq;
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
            ScheduledActivities = new Stack<ScheduledActivity>();
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
        public Stack<ScheduledActivity> ScheduledActivities { get; set; }

        /// <summary>
        /// Remove empty activity data to save on document size.
        /// </summary>
        internal void PruneActivityData()
        {
            ActivityData.Prune(x => x.Value.Count == 0);
            ActivityOutput.Prune(x => x.Value == null);
        }
    }
}