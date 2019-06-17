using System;
using System.Collections.Generic;
using System.Linq;
using NodaTime;

namespace Elsa.Models
{
    public class Workflow
    {
        public Workflow(IEnumerable<IActivity> activities, IEnumerable<Connection> connections) : this()
        {
            Activities = activities.ToList();
            Connections = connections.ToList();
        }

        public Workflow()
        {
            Id = Guid.NewGuid().ToString("N");
            CurrentScope = new WorkflowExecutionScope();
            Scopes = new Stack<WorkflowExecutionScope>(new[] { CurrentScope });
            Arguments = new Variables();
            BlockingActivities = new List<IActivity>();
            ExecutionLog = new List<LogEntry>();
        }

        public string Id { get; set; }
        public string ParentId { get; set; }
        public WorkflowStatus Status { get; set; }
        public Instant CreatedAt { get; set; }
        public Instant? StartedAt { get; set; }
        public Instant? HaltedAt { get; set; }
        public Instant? FinishedAt { get; set; }
        public ICollection<IActivity> Activities { get; set; } = new HashSet<IActivity>();
        public IList<Connection> Connections { get; set; } = new List<Connection>();
        public Stack<WorkflowExecutionScope> Scopes { get; set; }
        public WorkflowExecutionScope CurrentScope { get; set; }
        public Variables Arguments { get; set; }
        public IList<IActivity> BlockingActivities { get; set; }
        public IList<LogEntry> ExecutionLog { get; set; }
        public WorkflowFault Fault { get; set; }
    }
}