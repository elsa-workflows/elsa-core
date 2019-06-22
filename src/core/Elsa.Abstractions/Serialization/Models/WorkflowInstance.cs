using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Models;
using Elsa.Services.Models;
using NodaTime;

namespace Elsa.Serialization.Models
{
    public class WorkflowInstance
    {
        public WorkflowInstance(IEnumerable<ActivityDefinition> activities, IEnumerable<Connection> connections) : this()
        {
            Activities = activities.ToList();
            Connections = connections.ToList();
        }

        public WorkflowInstance()
        {
            Id = Guid.NewGuid().ToString("N");
            CurrentScope = new WorkflowExecutionScope();
            Scopes = new Stack<WorkflowExecutionScope>(new[] { CurrentScope });
            Arguments = new Variables();
            BlockingActivities = new HashSet<string>();
            ExecutionLog = new List<LogEntry>();
        }

        public string Id { get; set; }
        public string DefinitionId { get; set; }
        public WorkflowStatus Status { get; set; }
        public Instant CreatedAt { get; set; }
        public Instant? StartedAt { get; set; }
        public Instant? HaltedAt { get; set; }
        public Instant? FinishedAt { get; set; }
        public ICollection<ActivityDefinition> Activities { get; set; } = new List<ActivityDefinition>();
        public IList<Connection> Connections { get; set; } = new List<Connection>();
        public Stack<WorkflowExecutionScope> Scopes { get; set; }
        public WorkflowExecutionScope CurrentScope { get; set; }
        public Variables Arguments { get; set; }
        public HashSet<string> BlockingActivities { get; set; }
        public IList<LogEntry> ExecutionLog { get; set; }
        public WorkflowFault Fault { get; set; }
    }
}