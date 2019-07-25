using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Comparers;
using Elsa.Models;
using Newtonsoft.Json.Linq;
using NodaTime;

namespace Elsa.Services.Models
{
    public class Workflow
    {
        public Workflow(
            string id,
            string definitionId,
            int version,
            IEnumerable<IActivity> activities,
            IEnumerable<Connection> connections,
            Variables input = default,
            string correlationId = default) : this()
        {
            Id = id;
            DefinitionId = definitionId;
            Version = version;
            CorrelationId = correlationId;
            Activities = activities.ToList();
            Connections = connections.ToList();
            Input = new Variables(input ?? Variables.Empty);
        }

        public Workflow()
        {
            Scopes = new Stack<WorkflowExecutionScope>(new[] { new WorkflowExecutionScope() });
            BlockingActivities = new HashSet<IActivity>();
            ExecutionLog = new List<LogEntry>();
        }

        public string Id { get; set; }
        public string DefinitionId { get; }
        public int Version { get; }
        public string CorrelationId { get; set; }
        public WorkflowStatus Status { get; set; }
        public Instant CreatedAt { get; set; }
        public Instant? StartedAt { get; set; }
        public Instant? HaltedAt { get; set; }
        public Instant? FinishedAt { get; set; }
        public ICollection<IActivity> Activities { get; } = new List<IActivity>();
        public IList<Connection> Connections { get; } = new List<Connection>();
        public Stack<WorkflowExecutionScope> Scopes { get; set; }
        public HashSet<IActivity> BlockingActivities { get; set; }
        public IList<LogEntry> ExecutionLog { get; set; }
        public WorkflowFault Fault { get; set; }
        public Variables Input { get; set; }

        public WorkflowInstance ToInstance()
        {
            var activities = Activities.ToDictionary(x => x.Id, x => x.ToInstance());

            return new WorkflowInstance
            {
                Id = Id,
                DefinitionId = DefinitionId,
                Version = Version,
                CorrelationId = CorrelationId,
                Status = Status,
                CreatedAt = CreatedAt,
                StartedAt = StartedAt,
                HaltedAt = HaltedAt,
                FinishedAt = FinishedAt,
                Activities = activities,
                Scopes = new Stack<WorkflowExecutionScope>(Scopes),
                BlockingActivities = new HashSet<BlockingActivity>(BlockingActivities.Select(x => new BlockingActivity(x.Id, x.Type)), new BlockingActivityEqualityComparer()),
                ExecutionLog = ExecutionLog.ToList(),
                Fault = Fault?.ToInstance() 
            };
        }

        public void Initialize(WorkflowInstance instance)
        {
            if(instance == null)
                throw new ArgumentNullException(nameof(instance));

            var activityLookup = Activities.ToDictionary(x => x.Id);
            
            Id = instance.Id;
            CorrelationId = instance.CorrelationId;
            Status = instance.Status;
            CreatedAt = instance.CreatedAt;
            StartedAt = instance.StartedAt;
            HaltedAt = instance.HaltedAt;
            FinishedAt = instance.FinishedAt;
            BlockingActivities = new HashSet<IActivity>(instance.BlockingActivities.Select(x => activityLookup[x.ActivityId]));
            Scopes = new Stack<WorkflowExecutionScope>(instance.Scopes);
            
            foreach (var activity in Activities)
            {
                activity.State = new JObject(instance.Activities[activity.Id].State);
            }
        }
    }
}