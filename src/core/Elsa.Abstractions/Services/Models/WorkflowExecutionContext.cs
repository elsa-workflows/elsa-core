using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Comparers;
using Elsa.Expressions;
using Elsa.Models;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json.Linq;
using NodaTime;

namespace Elsa.Services.Models
{
    public class WorkflowExecutionContext
    {
        public WorkflowExecutionContext(IExpressionEvaluator expressionEvaluator,
            IClock clock,
            IServiceProvider serviceProvider,
            string definitionId,
            string instanceId,
            int version,
            IEnumerable<IActivity> activities,
            IEnumerable<Connection> connections,
            IEnumerable<ScheduledActivity>? scheduledActivities = default,
            IEnumerable<IActivity>? blockingActivities = default,
            string? correlationId = default,
            Variables? variables = default,
            WorkflowStatus status = WorkflowStatus.Running,
            WorkflowPersistenceBehavior persistenceBehavior = WorkflowPersistenceBehavior.WorkflowExecuted,
            
            IDictionary<string, IDictionary<string, IActivityPropertyValueProvider>>? activityPropertyValueProviders =
                default,
            
            WorkflowFault? workflowFault = default,
            IEnumerable<ExecutionLogEntry>? executionLog = default)
        {
            ServiceProvider = serviceProvider;
            DefinitionId = definitionId;
            InstanceId = instanceId;
            Version = version;
            CorrelationId = correlationId;
            Activities = activities.ToList();
            Connections = connections.ToList();
            ExpressionEvaluator = expressionEvaluator;
            Clock = clock;

            ScheduledActivities = scheduledActivities != null
                ? new Stack<ScheduledActivity>(scheduledActivities)
                : new Stack<ScheduledActivity>();

            BlockingActivities = blockingActivities != null
                ? new HashSet<IActivity>(blockingActivities)
                : new HashSet<IActivity>();

            Variables = variables ?? new Variables();
            Status = status;
            PersistenceBehavior = persistenceBehavior;

            ActivityPropertyValueProviders =
                activityPropertyValueProviders ??
                new Dictionary<string, IDictionary<string, IActivityPropertyValueProvider>>();

            WorkflowFault = workflowFault;
            ExecutionLog = executionLog?.ToList() ?? new List<ExecutionLogEntry>();
            IsFirstPass = true;
        }

        public IServiceProvider ServiceProvider { get; }
        public string DefinitionId { get; }
        public ICollection<IActivity> Activities { get; }
        public ICollection<Connection> Connections { get; }
        public WorkflowStatus Status { get; set; }
        public Stack<ScheduledActivity> ScheduledActivities { get; }
        public HashSet<IActivity> BlockingActivities { get; }
        public Variables Variables { get; }
        public bool HasScheduledActivities => ScheduledActivities.Any();
        public ScheduledActivity? ScheduledActivity { get; private set; }
        public WorkflowFault? WorkflowFault { get; private set; }
        public object? Output { get; set; }

        public void ScheduleActivities(IEnumerable<IActivity> activities, object? input = default)
        {
            foreach (var activity in activities)
                ScheduleActivity(activity, input);
        }

        public void ScheduleActivities(IEnumerable<ScheduledActivity> activities)
        {
            foreach (var activity in activities)
                ScheduleActivity(activity);
        }

        public void ScheduleActivity(IActivity activity, object? input = default) =>
            ScheduleActivity(new ScheduledActivity(activity, input));

        public void ScheduleActivity(ScheduledActivity activity) => ScheduledActivities.Push(activity);
        public ScheduledActivity PopScheduledActivity() => ScheduledActivity = ScheduledActivities.Pop();
        public ScheduledActivity PeekScheduledActivity() => ScheduledActivities.Peek();
        public IExpressionEvaluator ExpressionEvaluator { get; }
        public IClock Clock { get; }
        public string InstanceId { get; set; }
        public int Version { get; }
        public string? CorrelationId { get; set; }
        public WorkflowPersistenceBehavior PersistenceBehavior { get; set; }

        public IDictionary<string, IDictionary<string, IActivityPropertyValueProvider>> ActivityPropertyValueProviders
        {
            get;
        }

        public bool DeleteCompletedInstances { get; set; }
        public ICollection<ExecutionLogEntry> ExecutionLog { get; }
        public bool IsFirstPass { get; private set; }

        public bool AddBlockingActivity(IActivity activity) => BlockingActivities.Add(activity);
        public void SetVariable(string name, object? value) => Variables.Set(name, JToken.FromObject(value));
        public T GetVariable<T>(string name) => (T)GetVariable(name)!;
        public object? GetVariable(string name) => Variables.Get(name);
        public void CompletePass() => IsFirstPass = false;

        public void Suspend() => Status = WorkflowStatus.Suspended;

        public void Fault(IActivity? activity, LocalizedString? message)
        {
            Status = WorkflowStatus.Faulted;
            WorkflowFault = new WorkflowFault(activity, message);
        }

        public void Complete() => Status = WorkflowStatus.Completed;

        public IActivity? GetActivity(string id) => Activities.FirstOrDefault(x => x.Id == id);

        public WorkflowInstance CreateWorkflowInstance()
        {
            return UpdateWorkflowInstance(
                new WorkflowInstance
                {
                    Id = InstanceId,
                    DefinitionId = DefinitionId,
                    Version = Version,
                    CreatedAt = Clock.GetCurrentInstant()
                });
        }

        public WorkflowInstance UpdateWorkflowInstance(WorkflowInstance workflowInstance)
        {
            workflowInstance.Variables = Variables;
            workflowInstance.ScheduledActivities = new Stack<Elsa.Models.ScheduledActivity>(
                ScheduledActivities.Select(x => new Elsa.Models.ScheduledActivity(x.Activity.Id, x.Input)));
            workflowInstance.Activities = Activities.Select(x => new ActivityInstanceRecord(x.Id, x.Type, x.Output)).ToList();
            workflowInstance.BlockingActivities = new HashSet<BlockingActivity>(
                BlockingActivities.Select(x => new BlockingActivity(x.Id, x.Type)),
                new BlockingActivityEqualityComparer());
            workflowInstance.Status = Status;
            workflowInstance.CorrelationId = CorrelationId;
            workflowInstance.Output = Output;

            var executionLog = workflowInstance.ExecutionLog.Concat(
                ExecutionLog.Select(x => new Elsa.Models.ExecutionLogEntry(x.Activity.Id, x.Timestamp)));
            workflowInstance.ExecutionLog = executionLog.ToList();

            if (WorkflowFault != null)
            {
                workflowInstance.Fault = new Elsa.Models.WorkflowFault
                {
                    FaultedActivityId = WorkflowFault.FaultedActivity?.Id,
                    Message = WorkflowFault.Message
                };
            }

            return workflowInstance;
        }
    }
}