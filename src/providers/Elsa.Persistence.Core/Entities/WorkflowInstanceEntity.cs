using Elsa.Models;
using System;
using System.Collections.Generic;

namespace Elsa.Persistence.Core.Entities
{
    public class WorkflowInstanceEntity
    {
        public int Id { get; set; }
        public string InstanceId { get; set; }= default!;
        public string DefinitionId { get; set; }= default!;
        public int Version { get; set; }
        public WorkflowStatus Status { get; set; }
        public string CorrelationId { get; set; }= default!;
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public DateTime? FaultedAt { get; set; }
        public DateTime? AbortedAt { get; set; }
        public Variables Variables { get; set; }= default!;
        public Variables Input { get; set; }= default!;
        public ICollection<ExecutionLogEntry> ExecutionLog { get; set; }= default!;
        public WorkflowFault Fault { get; set; }= default!;
        public ICollection<ActivityInstanceEntity> Activities { get; set; }= default!;
        public ICollection<BlockingActivityEntity> BlockingActivities { get; set; }= default!;
        public Stack<ScheduledActivityEntity> ScheduledActivities { get; set; }= default!;
    }
}