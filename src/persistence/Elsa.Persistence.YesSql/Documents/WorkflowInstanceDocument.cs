using System;
using System.Collections.Generic;
using Elsa.Models;

namespace Elsa.Persistence.YesSql.Documents
{
    public class WorkflowInstanceDocument
    {
        public int Id { get; set; }
        public string WorkflowInstanceId { get; set; }
        public string DefinitionId { get; set; }
        public int Version { get; set; }
        public WorkflowStatus Status { get; set; }
        public string CorrelationId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public DateTime? FaultedAt { get; set; }
        public DateTime? AbortedAt { get; set; }
        public IDictionary<string, ActivityInstance> Activities { get; set; } = new Dictionary<string, ActivityInstance>();
        public WorkflowExecutionScope Scope { get; set; }
        public Variables Input { get; set; }
        public HashSet<BlockingActivity> BlockingActivities { get; set; }
        public ICollection<LogEntry> ExecutionLog { get; set; }
        public WorkflowFault Fault { get; set; }
    }
}
