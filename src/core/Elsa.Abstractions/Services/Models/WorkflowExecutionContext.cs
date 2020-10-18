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
            WorkflowInstance workflowInstance
        )
        {
            ServiceProvider = serviceProvider;
            WorkflowBlueprint = workflowBlueprint;
            WorkflowInstance = workflowInstance;
            ExecutionLog = new List<ExecutionLogEntry>(workflowInstance.ExecutionLog);
            IsFirstPass = true;
        }

        public IWorkflowBlueprint WorkflowBlueprint { get; }
        public IServiceProvider ServiceProvider { get; }
        public WorkflowInstance WorkflowInstance { get; }
        public bool HasScheduledActivities => WorkflowInstance.ScheduledActivities.Any();
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

        public void ScheduleActivity(string activityId, object? input = default) => ScheduleActivity(new ScheduledActivity(activityId, input));
        public void ScheduleActivity(ScheduledActivity activity) => WorkflowInstance.ScheduledActivities.Push(activity);
        public ScheduledActivity PopScheduledActivity() => WorkflowInstance.ScheduledActivities.Pop();
        public ScheduledActivity PeekScheduledActivity() => WorkflowInstance.ScheduledActivities.Peek();
        public string? CorrelationId { get; set; }
        public bool DeleteCompletedInstances { get; set; }
        public ICollection<ExecutionLogEntry> ExecutionLog { get; }
        public WorkflowStatus Status => WorkflowInstance.Status;

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

        public void Complete() => WorkflowInstance.Status = WorkflowStatus.Completed;

        public IActivityBlueprint? GetActivityBlueprint(string id) =>
            WorkflowBlueprint.Activities.FirstOrDefault(x => x.Id == id);

        private JObject Serialize(IActivity activity) => JObject.FromObject(activity);
    }
}