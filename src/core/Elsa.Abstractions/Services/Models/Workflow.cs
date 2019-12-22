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
            WorkflowBlueprint blueprint,
            Instant createdAt,
            Variables input = default,
            string correlationId = default) : this(blueprint)
        {
            Id = id;
            CreatedAt = createdAt;
            CorrelationId = correlationId;
            Input = new Variables(input ?? Variables.Empty);
        }

        public Workflow(WorkflowBlueprint blueprint)
        {
            Blueprint = blueprint;
            Scopes = new Stack<WorkflowExecutionScope>(new[] { new WorkflowExecutionScope() });
            BlockingActivities = new HashSet<IActivity>();
            ScheduledActivities = new Stack<IActivity>();
            ExecutionLog = new List<LogEntry>();
        }

        public string Id { get; set; }
        public WorkflowBlueprint Blueprint { get; }
        public string CorrelationId { get; set; }
        public WorkflowStatus Status { get; set; }
        public Instant CreatedAt { get; set; }
        public Instant? StartedAt { get; set; }
        public Instant? CompletedAt { get; set; }
        public Instant? FaultedAt { get; set; }
        public Instant? CancelledAt { get; set; }
        public Stack<WorkflowExecutionScope> Scopes { get; set; }
        public Stack<IActivity> ScheduledActivities { get; set; }
        public HashSet<IActivity> BlockingActivities { get; set; }
        public IList<LogEntry> ExecutionLog { get; set; }
        public WorkflowFault Fault { get; set; }
        public Variables Input { get; set; }
        public Variables Output { get; set; }

        public WorkflowInstance ToInstance()
        {
            return new WorkflowInstance
            {
                Id = Id,
                DefinitionId = Blueprint.DefinitionId,
                Version = Blueprint.Version,
                CorrelationId = CorrelationId,
                Status = Status,
                CreatedAt = CreatedAt,
                StartedAt = StartedAt,
                FinishedAt = CompletedAt,
                FaultedAt = FaultedAt,
                AbortedAt = CancelledAt,
                Activities = new HashSet<ActivityInstance>(Blueprint.Activities.Select(x => x.ToInstance()), new ActivityInstanceEqualityComparer()),
                Scopes = new Stack<WorkflowExecutionScope>(Scopes),
                BlockingActivities = new HashSet<BlockingActivity>(BlockingActivities.Select(x => new BlockingActivity(x.Id, x.Type)), new BlockingActivityEqualityComparer()),
                ScheduledActivities = new HashSet<string>(ScheduledActivities.Select(x => x.Id)),
                ExecutionLog = ExecutionLog.ToList(),
                Fault = Fault?.ToInstance()
            };
        }

        public void Initialize(WorkflowInstance instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            var activityLookup = Blueprint.Activities.ToDictionary(x => x.Id);

            Id = instance.Id;
            CorrelationId = instance.CorrelationId;
            Status = instance.Status;
            CreatedAt = instance.CreatedAt;
            StartedAt = instance.StartedAt;
            CompletedAt = instance.FinishedAt;
            FaultedAt = instance.FaultedAt;
            CancelledAt = instance.AbortedAt;
            ExecutionLog = instance.ExecutionLog.ToList();
            BlockingActivities = new HashSet<IActivity>(instance.BlockingActivities.Select(x => activityLookup[x.ActivityId]));
            ScheduledActivities = new Stack<IActivity>(instance.ScheduledActivities.Select(x => activityLookup[x]));
            Scopes = new Stack<WorkflowExecutionScope>(instance.Scopes);

            var activityDictionary = instance.Activities.ToDictionary(x => x.Id);
            
            foreach (var activity in Blueprint.Activities)
            {
                activity.State = new JObject(activityDictionary[activity.Id].State);
                activity.Output = activityDictionary[activity.Id].Output?.ToObject<Variable>();
            }
        }
    }
}