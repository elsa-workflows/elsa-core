using System.Collections.Generic;
using Elsa.Models;
using NodaTime;

namespace Elsa.Serialization.Models
{
    public class WorkflowInstance
    {
        public string Id { get; set; }
        public string DefinitionId { get; set; }
        public WorkflowStatus Status { get; set; }
        public Instant CreatedAt { get; set; }
        public Instant? StartedAt { get; set; }
        public Instant? HaltedAt { get; set; }
        public Instant? FinishedAt { get; set; }
        public IDictionary<string, ActivityInstance> Activities { get; set; } = new Dictionary<string, ActivityInstance>();
        public Stack<WorkflowExecutionScope> Scopes { get; set; }
        public Variables Input { get; set; }
        public HashSet<string> BlockingActivities { get; set; }
        public ICollection<LogEntry> ExecutionLog { get; set; }
        public WorkflowFault Fault { get; set; }
    }
}