using Elsa.Models;
using System;
using System.Collections.Generic;

namespace Elsa.Persistence.EntityFrameworkCore.Entities
{
    public class WorkflowInstanceEntity
    {
        public int Id { get; set; }
        public string? InstanceId { get; set; }
        public string? DefinitionId { get; set; }
        public string? CorrelationId { get; set; }
        public int? Version { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public DateTime? FaultedAt { get; set; }
        public DateTime? AbortedAt { get; set; }
        public WorkflowStatus? Status { get; set; }
        public WorkflowFault? Fault { get; set; }
        public Variables? Variables { get; set; }
        public Variables? Input { get; set; }
        public ICollection<ExecutionLogEntry> ExecutionLog { get; set; }
        public ICollection<ActivityInstanceEntity> Activities { get; set; }
        public ICollection<BlockingActivityEntity> BlockingActivities { get; set; }
        public Stack<ScheduledActivityEntity> ScheduledActivities { get; set; }
    }
}