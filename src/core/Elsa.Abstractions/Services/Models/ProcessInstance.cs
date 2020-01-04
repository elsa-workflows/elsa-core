using System.Collections.Generic;
using Elsa.Models;
using NodaTime;

namespace Elsa.Services.Models
{
    public class ProcessInstance
    {
        public ProcessInstance(
            string id,
            Workflow blueprint,
            Instant createdAt,
            Variable? input = default,
            string? correlationId = default) : this(blueprint)
        {
            Id = id;
            CreatedAt = createdAt;
            CorrelationId = correlationId;
            Input = input;
        }

        public ProcessInstance(Workflow blueprint)
        {
            Blueprint = blueprint;
            Scopes = new Stack<WorkflowExecutionScope>(new[] { new WorkflowExecutionScope() });
            BlockingActivities = new HashSet<IActivity>();
            ScheduledActivities = new Stack<ScheduledActivity>();
            ExecutionLog = new List<LogEntry>();
        }

        public string Id { get; private set; }
        public Workflow Blueprint { get; }
        public string CorrelationId { get; set; }
        public WorkflowStatus Status { get; set; }
        public Instant CreatedAt { get; set; }
        public Instant? StartedAt { get; set; }
        public Instant? CompletedAt { get; set; }
        public Instant? FaultedAt { get; set; }
        public Instant? CancelledAt { get; set; }
        public Stack<WorkflowExecutionScope> Scopes { get; set; }
        public Stack<ScheduledActivity> ScheduledActivities { get; set; }
        public HashSet<IActivity> BlockingActivities { get; set; }
        public IList<LogEntry> ExecutionLog { get; set; }
        public ProcessFault Fault { get; set; }
        public Variable? Input { get; set; }
        public Variable? Output { get; set; }
    }
}