using System.Collections.Generic;
using AutoMapper.Configuration.Conventions;
using Elsa.Comparers;
using Elsa.Models;
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
        public string? ContextId { get; set; }
        public Instant CreatedAt { get; set; }
        public Instant? LastExecutedAt { get; set; }
        public Instant? FinishedAt { get; set; }
        public Instant? CancelledAt { get; set; }
        public Instant? FaultedAt { get; set; }
        public Variables Variables { get; set; } = new();
        public object? Output { get; set; }
        public ICollection<ActivityInstance> Activities { get; set; } = new List<ActivityInstance>();

        public HashSet<BlockingActivity> BlockingActivities
        {
            get => _blockingActivities;
            set => _blockingActivities = new HashSet<BlockingActivity>(value, BlockingActivityEqualityComparer.Instance);
        }

        public ICollection<WorkflowExecutionLogRecord> ExecutionLog { get; set; } = new List<WorkflowExecutionLogRecord>();
        public WorkflowFault? Fault { get; set; }
        public Stack<ScheduledActivity> ScheduledActivities { get; set; } = new();
        public Stack<ScheduledActivity> PostScheduledActivities { get; set; } = new();
        public Stack<string> ParentActivities { get; set; } = new();
    }
}