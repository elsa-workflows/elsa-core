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
            WorkflowDefinitionVersion definition,
            Instant createdAt,
            IEnumerable<IActivity> activities,
            IEnumerable<Connection> connections,
            Variables input = default,
            string correlationId = default) : this()
        {
            Id = id;
            Definition = definition;
            CreatedAt = createdAt;
            CorrelationId = correlationId;
            Activities = activities.ToList();
            Connections = connections.ToList();
            Input = new Variables().SetVariables(definition.Variables).SetVariables(new Variables(input ?? Variables.Empty));
        }

        public Workflow()
        {
            Scope = new WorkflowExecutionScope();
            BlockingActivities = new HashSet<IActivity>();
            ExecutionLog = new List<LogEntry>();
        }

        public string Id { get; set; }
        public WorkflowDefinitionVersion Definition { get; }
        public string CorrelationId { get; set; }
        public WorkflowStatus Status { get; set; }
        public Instant CreatedAt { get; set; }
        public Instant? StartedAt { get; set; }
        public Instant? FinishedAt { get; set; }
        public Instant? FaultedAt { get; set; }
        public Instant? AbortedAt { get; set; }
        public ICollection<IActivity> Activities { get; } = new List<IActivity>();
        public IList<Connection> Connections { get; } = new List<Connection>();
        public WorkflowExecutionScope Scope { get; set; }
        public HashSet<IActivity> BlockingActivities { get; set; }
        public IList<LogEntry> ExecutionLog { get; set; }
        public WorkflowFault Fault { get; set; }
        public Variables Input { get; set; }
        public Variables Output { get; set; }

        public WorkflowInstance ToInstance()
        {
            var activities = Activities.ToDictionary(x => x.Id, x => x.ToInstance());

            return new WorkflowInstance
            {
                Id = Id,
                DefinitionId = Definition.DefinitionId,
                Version = Definition.Version,
                CorrelationId = CorrelationId,
                Status = Status,
                CreatedAt = CreatedAt,
                StartedAt = StartedAt,
                FinishedAt = FinishedAt,
                FaultedAt = FaultedAt,
                AbortedAt = AbortedAt,
                Activities = activities,
                Scope = Scope,
                Input = Input,

                BlockingActivities = new HashSet<BlockingActivity>(
                    BlockingActivities.Select(x => new BlockingActivity(x.Id, x.Type)),
                    new BlockingActivityEqualityComparer()
                ),
                
                ExecutionLog = ExecutionLog.ToList(),
                Fault = Fault?.ToInstance()
            };
        }

        public void Initialize(WorkflowInstance instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            var activityLookup = Activities.ToDictionary(x => x.Id);

            Id = instance.Id;
            CorrelationId = instance.CorrelationId;
            Status = instance.Status;
            CreatedAt = instance.CreatedAt;
            StartedAt = instance.StartedAt;
            FinishedAt = instance.FinishedAt;
            FaultedAt = instance.FaultedAt;
            AbortedAt = instance.AbortedAt;
            ExecutionLog = instance.ExecutionLog.ToList();
            Scope = instance.Scope;
            Input = new Variables(Input.Concat(instance.Input)
               .GroupBy(kv => kv.Key)
               .ToDictionary(g => g.Key, g => g.First().Value));
            
            BlockingActivities =
                new HashSet<IActivity>(instance.BlockingActivities.Select(x => activityLookup[x.ActivityId]));
            
            foreach (var activity in Activities)
            {
                activity.State = new JObject(instance.Activities[activity.Id].State);
                activity.Output = instance.Activities[activity.Id].Output.ToObject<Variables>();
            }
        }
    }
}
