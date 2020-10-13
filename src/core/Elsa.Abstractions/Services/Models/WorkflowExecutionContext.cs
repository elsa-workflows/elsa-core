using System;
using System.Collections.Generic;
using System.Linq;
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
        public WorkflowExecutionContext(
            IExpressionEvaluator expressionEvaluator,
            IServiceProvider serviceProvider,
            IWorkflowBlueprint workflowBlueprint,
            WorkflowInstance workflowInstance
            //IWorkflow workflow,
            //WorkflowStatus status,
            //Variables variables,
            //string correlationId,
            //IWorkflowFault? workflowFault,
            //ICollection<IScheduledActivity> scheduledActivities,
            //ICollection<IActivity> blockingActivities,
            //IEnumerable<IExecutionLogEntry>? executionLog = default
            )
        {
            ServiceProvider = serviceProvider;
            WorkflowBlueprint = workflowBlueprint;
            //WorkflowDefinition = workflowDefinition;
            WorkflowInstance = workflowInstance;
            //Workflow = workflow;
            //CorrelationId = correlationId;
            ExpressionEvaluator = expressionEvaluator;
            //ScheduledActivities = new Stack<IScheduledActivity>(scheduledActivities.Reverse());
            //BlockingActivities = new HashSet<IActivity>(blockingActivities);
            //Variables = variables;
            //Status = status;
            //PersistenceBehavior = workflow.PersistenceBehavior;
            //ActivityPropertyProviders = workflow.ActivityPropertyProviders;
            //WorkflowFault = workflowFault;
            //ExecutionLog = executionLog?.ToList() ?? new List<IExecutionLogEntry>();
            IsFirstPass = true;
        }

        private IScheduledActivity CreateScheduledActivity(Elsa.Models.ScheduledActivity scheduledActivityModel) =>
            new ScheduledActivity(scheduledActivityModel.ActivityId, scheduledActivityModel.Input);

        public IWorkflowBlueprint WorkflowBlueprint { get; }
        public IServiceProvider ServiceProvider { get; }
        // public WorkflowDefinition WorkflowDefinition { get; }
        public WorkflowInstance WorkflowInstance { get; }
        public WorkflowStatus Status { get; set; }
        public Stack<IScheduledActivity> ScheduledActivities { get; }
        public HashSet<BlockingActivity> BlockingActivities { get; } = new HashSet<BlockingActivity>(new BlockingActivityEqualityComparer());
        public Variables Variables { get; }
        public bool HasScheduledActivities => ScheduledActivities.Any();
        public IScheduledActivity? ScheduledActivity { get; private set; }
        public IWorkflowFault? WorkflowFault { get; private set; }
        public object? Output { get; set; }

        public void ScheduleActivities(IEnumerable<string> activityIds, object? input = default)
        {
            foreach (var activityId in activityIds)
                ScheduleActivity(activityId, input);
        }

        public void ScheduleActivities(IEnumerable<ScheduledActivity> activities)
        {
            foreach (var activity in activities)
                ScheduleActivity(activity);
        }

        public void ScheduleActivity(string activityId, object? input = default) =>
            ScheduleActivity(new ScheduledActivity(activityId, input));

        public void ScheduleActivity(ScheduledActivity activity) => ScheduledActivities.Push(activity);
        public IScheduledActivity PopScheduledActivity() => ScheduledActivity = ScheduledActivities.Pop();
        public IScheduledActivity PeekScheduledActivity() => ScheduledActivities.Peek();
        public IExpressionEvaluator ExpressionEvaluator { get; }
        public string? CorrelationId { get; set; }
        public WorkflowPersistenceBehavior PersistenceBehavior { get; }
        public IActivityPropertyProviders ActivityPropertyProviders { get; }
        public bool DeleteCompletedInstances { get; set; }
        public ICollection<IExecutionLogEntry> ExecutionLog { get; }
        public bool IsFirstPass { get; private set; }

        public bool AddBlockingActivity(IActivity activity) => BlockingActivities.Add(new BlockingActivity(activity.Id, activity.Type));
        public void SetVariable(string name, object? value) => Variables.Set(name, JToken.FromObject(value!));
        public T GetVariable<T>(string name) => (T)GetVariable(name)!;
        public object? GetVariable(string name) => Variables.Get(name);
        public void CompletePass() => IsFirstPass = false;

        public void Suspend() => Status = WorkflowStatus.Suspended;

        public void Fault(string? activityId, LocalizedString? message)
        {
            Status = WorkflowStatus.Faulted;
            WorkflowFault = new WorkflowFault(activityId, message);
        }

        public void Complete() => Status = WorkflowStatus.Completed;

        public IActivityBlueprint? GetActivity(string id) => WorkflowBlueprint.Activities.FirstOrDefault(x => x.Id == id);

        public void UpdateWorkflowInstance(WorkflowInstance workflowInstance)
        {
            workflowInstance.Variables = Variables;

            workflowInstance.ScheduledActivities = new Stack<Elsa.Models.ScheduledActivity>(
                ScheduledActivities.Select(x => new Elsa.Models.ScheduledActivity(x.ActivityId, x.Input)));

            //workflowInstance.Activities =
            //    WorkflowBlueprint.Activities.Select(x => new ActivityInstance(x.Id, x.Type, x.Output, Serialize(x))).ToList();

            workflowInstance.BlockingActivities = BlockingActivities;
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
                    FaultedActivityId = WorkflowFault.FaultedActivityId,
                    Message = WorkflowFault.Message
                };
            }
        }

        private JObject Serialize(IActivity activity) => JObject.FromObject(activity);
    }
}