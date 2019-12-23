using System.Collections.Generic;
using NodaTime;

namespace Elsa.Models
{
    public class WorkflowInstance
    {
        public WorkflowInstance()
        {
            Activities = new List<ActivityInstance>();
            Scopes = new Stack<WorkflowExecutionScope>();
            BlockingActivities = new HashSet<BlockingActivity>();
            ExecutionLog = new List<LogEntry>();
            ScheduledActivities = new HashSet<ScheduledActivity>();
        }
        
        public string? Id { get; set; }
        public string? DefinitionId { get; set; }
        public int Version { get; set; }
        public WorkflowStatus Status { get; set; }
        public string? CorrelationId { get; set; }
        public Instant CreatedAt { get; set; }
        public Instant? StartedAt { get; set; }
        public Instant? FinishedAt { get; set; }
        public Instant? FaultedAt { get; set; }
        public Instant? AbortedAt { get; set; }
        public ICollection<ActivityInstance> Activities { get; set; }
        public Stack<WorkflowExecutionScope> Scopes { get; set; }
        public Variable? Input { get; set; }
        public Variable? Output { get; set; }
        public HashSet<BlockingActivity> BlockingActivities { get; set; }
        public ICollection<LogEntry> ExecutionLog { get; set; }
        public WorkflowFault? Fault { get; set; }
        public HashSet<ScheduledActivity> ScheduledActivities { get; set; }
    }
}