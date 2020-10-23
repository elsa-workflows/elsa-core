using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Models;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json.Linq;

namespace Elsa.Services.Models
{
    public class WorkflowExecutionContext
    {
        public WorkflowExecutionContext(
            IServiceProvider serviceProvider,
            IWorkflowBlueprint workflowBlueprint,
            WorkflowInstance workflowInstance,
            object? input
        )
        {
            ServiceProvider = serviceProvider;
            WorkflowBlueprint = workflowBlueprint;
            WorkflowInstance = workflowInstance;
            Input = input;
            IsFirstPass = true;
        }

        public IWorkflowBlueprint WorkflowBlueprint { get; }
        public IServiceProvider ServiceProvider { get; }
        public WorkflowInstance WorkflowInstance { get; }
        public object? Input { get; }
        public bool HasScheduledActivities => WorkflowInstance.ScheduledActivities.Any();
        public bool HasPostScheduledActivities => WorkflowInstance.PostScheduledActivities.Any();
        public IWorkflowFault? WorkflowFault { get; private set; }
        public bool IsFirstPass { get; private set; }

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
        
        public void PostScheduleActivities(IEnumerable<ScheduledActivity> activities)
        {
            foreach (var activity in activities)
                PostScheduleActivity(activity);
        }

        public void ScheduleActivity(string activityId, object? input = default) => ScheduleActivity(new ScheduledActivity(activityId, input));
        public void ScheduleActivity(ScheduledActivity activity) => WorkflowInstance.ScheduledActivities.Push(activity);
        public void PostScheduleActivity(string activityId, object? input = default) => PostScheduleActivity(new ScheduledActivity(activityId, input));
        public void PostScheduleActivity(ScheduledActivity activity) => WorkflowInstance.PostScheduledActivities.Push(activity);
        public ScheduledActivity PopScheduledActivity() => WorkflowInstance.ScheduledActivities.Pop();
        public ScheduledActivity PeekScheduledActivity() => WorkflowInstance.ScheduledActivities.Peek();
        public string? CorrelationId { get; set; }
        public bool DeleteCompletedInstances { get; set; }
        public ICollection<ExecutionLogEntry> ExecutionLog => WorkflowInstance.ExecutionLog;
        public WorkflowStatus Status => WorkflowInstance.Status;
        public bool HasBlockingActivities => WorkflowInstance.BlockingActivities.Any();

        public void SetVariable(string name, object? value) => WorkflowInstance.Variables.Set(name, JToken.FromObject(value!));
        public T GetVariable<T>(string name) => WorkflowInstance.Variables.Get<T>(name);
        public object? GetVariable(string name) => WorkflowInstance.Variables.Get(name);
        public void CompletePass() => IsFirstPass = false;
        public void Begin() => WorkflowInstance.Status = WorkflowStatus.Running;
        public void Resume() => WorkflowInstance.Status = WorkflowStatus.Running;
        public void Suspend() => WorkflowInstance.Status = WorkflowStatus.Suspended;

        public void Fault(string? activityId, LocalizedString? message)
        {
            WorkflowInstance.Status = WorkflowStatus.Faulted;
            WorkflowFault = new WorkflowFault(activityId, message);
        }

        public void Complete(object? output = default)
        {
            WorkflowInstance.Status = WorkflowStatus.Finished;
            WorkflowInstance.Output = output;
        }

        public IActivityBlueprint? GetActivityBlueprint(string id) =>
            WorkflowBlueprint.Activities.FirstOrDefault(x => x.Id == id);

        private JObject Serialize(IActivity activity) => JObject.FromObject(activity);

        public void SchedulePostActivities()
        {
            while(HasPostScheduledActivities)
                ScheduleActivity(WorkflowInstance.PostScheduledActivities.Pop());
        }
    }
}