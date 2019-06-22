using System.Collections.Generic;
using System.Linq;
using Elsa.Models;
using Elsa.Serialization.Models;
using NodaTime;

namespace Elsa.Services.Models
{
    public class Workflow
    {
        public Workflow(
            IEnumerable<IActivity> activities, 
            IEnumerable<Connection> connections,
            Variables input) : this()
        {
            Activities = activities.ToList();
            Connections = connections.ToList();
            Input = input;
        }

        public Workflow()
        {
            Scopes = new Stack<WorkflowExecutionScope>(new[] { new WorkflowExecutionScope() });
            Input = new Variables();
            BlockingActivities = new HashSet<IActivity>();
            ExecutionLog = new List<LogEntry>();
        }

        public string Id { get; set; }
        public WorkflowStatus Status { get; set; }
        public Instant CreatedAt { get; set; }
        public Instant? StartedAt { get; set; }
        public Instant? HaltedAt { get; set; }
        public Instant? FinishedAt { get; set; }
        public ICollection<IActivity> Activities { get; } = new List<IActivity>();
        public IList<Connection> Connections { get; } = new List<Connection>();
        public Stack<WorkflowExecutionScope> Scopes { get; }
        public Variables Input { get; set; }
        public HashSet<IActivity> BlockingActivities { get; set; }
        public IList<LogEntry> ExecutionLog { get; set; }
        public WorkflowFault Fault { get; set; }

        public WorkflowInstance Serialize()
        {
            return new WorkflowInstance();
        }
    }
}