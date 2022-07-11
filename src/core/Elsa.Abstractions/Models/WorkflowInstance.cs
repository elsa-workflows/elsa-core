using System;
using System.Collections.Generic;
using Elsa.Comparers;
using Elsa.Services.Models;
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
            Faults = new SimpleStack<WorkflowFault>();
        }

        public string DefinitionId { get; set; } = default!;
        public string DefinitionVersionId { get; set; } = default!;
        public string? TenantId { get; set; }
        public int Version { get; set; }
        
        public WorkflowStatus WorkflowStatus { get; set; }
        public string CorrelationId { get; set; } = default!;
        public string? ContextType { get; set; }
        public string? ContextId { get; set; }
        public string? Name { get; set; }
        public Instant CreatedAt { get; set; }
        public Instant? LastExecutedAt { get; set; }
        public Instant? FinishedAt { get; set; }
        public Instant? CancelledAt { get; set; }
        public Instant? FaultedAt { get; set; }
        public Variables Variables { get; set; }
        public WorkflowInputReference? Input { get; set; }
        public WorkflowOutputReference? Output { get; set; }
        public IDictionary<string, IDictionary<string, object?>> ActivityData { get; set; } = new Dictionary<string, IDictionary<string, object?>>();

        // To prevent NRE when old workflow instances are deserialized. 
        private IDictionary<string, object?>? _metadata;
        public IDictionary<string, object?> Metadata
        {
            get { return _metadata ??= new Dictionary<string, object?>(); }
            set => _metadata = value ?? new Dictionary<string, object?>();
        }

        public HashSet<BlockingActivity> BlockingActivities
        {
            get => _blockingActivities;
            set => _blockingActivities = new HashSet<BlockingActivity>(value, BlockingActivityEqualityComparer.Instance);
        }

        public object? GetMetadata(string key) => Metadata.TryGetValue(key, out var value) ? value : default;
        public void SetMetadata(string key, object? value)
        {
            Metadata[key] = value;
        }
        [Obsolete("This property is obsolete. Use Faults instead.", false)]
        public WorkflowFault? Fault { get; set; }

        private SimpleStack<WorkflowFault> _faults;
        public SimpleStack<WorkflowFault> Faults {
            get
            {
                return _faults ??= new SimpleStack<WorkflowFault>();
            }
            set
            {
                //Temporal patch to decrease the Faults change impact
                var result = value ?? new SimpleStack<WorkflowFault>();
                if (Fault != null)
                {
                    result.Push(Fault);
                    Fault = null;
                }
                _faults = result;
            }
        }
        public SimpleStack<ScheduledActivity> ScheduledActivities { get; set; }
        public SimpleStack<ActivityScope> Scopes { get; set; }
        public ScheduledActivity? CurrentActivity { get; set; }
        public string? LastExecutedActivityId { get; set; }
    }
}