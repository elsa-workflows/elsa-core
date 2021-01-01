using System.Collections.Generic;
using System.Runtime.Serialization;
using Elsa.Client.Comparers;
using NodaTime;

namespace Elsa.Client.Models
{
    [DataContract]
    public class WorkflowInstance
    {
        private HashSet<BlockingActivity> _blockingActivities = new(BlockingActivityEqualityComparer.Instance);

        public WorkflowInstance()
        {
            Variables = new Variables();
            Activities = new List<ActivityInstance>();
            ScheduledActivities = new Stack<ScheduledActivity>();
        }

        [DataMember(Order = 1)] public string Id { get; set; } = default!;
        [DataMember(Order = 2)] public string DefinitionId { get; set; } = default!;
        [DataMember(Order = 3)] public int Version { get; set; }
        [DataMember(Order = 4)] public WorkflowStatus WorkflowStatus { get; set; }
        [DataMember(Order = 5)] public string? CorrelationId { get; set; }
        [DataMember(Order = 6)] public string? ContextId { get; set; }
        [DataMember(Order = 7)] public string? Name { get; set; }
        [DataMember(Order = 8)] public Instant CreatedAt { get; set; }
        [DataMember(Order = 9)] public Instant? LastExecutedAt { get; set; }
        [DataMember(Order = 10)] public Instant? LastBurstAt { get; set; }
        [DataMember(Order = 11)] public Instant? FinishedAt { get; set; }
        [DataMember(Order = 12)] public Variables Variables { get; set; }
        [DataMember(Order = 13)] public object? Output { get; set; }
        [DataMember(Order = 14)] public ICollection<ActivityInstance> Activities { get; set; }

        [DataMember(Order = 15)]
        public HashSet<BlockingActivity> BlockingActivities
        {
            get => _blockingActivities;
            set => _blockingActivities = new HashSet<BlockingActivity>(value, BlockingActivityEqualityComparer.Instance);
        }
        
        [DataMember(Order = 16)] public WorkflowFault? Fault { get; set; }
        [DataMember(Order = 17)] public Stack<ScheduledActivity> ScheduledActivities { get; set; }
    }
}