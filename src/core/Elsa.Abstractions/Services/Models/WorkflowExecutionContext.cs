using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.ActivityProviders;
using Elsa.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;

namespace Elsa.Services.Models
{
    public class WorkflowExecutionContext
    {
        public WorkflowExecutionContext(
            IServiceScope serviceScope,
            IWorkflowBlueprint workflowBlueprint,
            WorkflowInstance workflowInstance,
            object? input = default
        )
        {
            ServiceScope = serviceScope;
            WorkflowBlueprint = workflowBlueprint;
            WorkflowInstance = workflowInstance;
            Input = input;
            IsFirstPass = true;
            Serializer = serviceScope.ServiceProvider.GetRequiredService<JsonSerializer>();
        }

        public IWorkflowBlueprint WorkflowBlueprint { get; }
        public IServiceScope ServiceScope { get; }
        public WorkflowInstance WorkflowInstance { get; }
        public JsonSerializer Serializer { get; }
        public object? Input { get; }
        public bool HasScheduledActivities => WorkflowInstance.ScheduledActivities.Any();
        public bool HasPostScheduledActivities => WorkflowInstance.PostScheduledActivities.Any();
        public IWorkflowFault? WorkflowFault { get; private set; }
        public bool IsFirstPass { get; private set; }
        public bool ContextHasChanged { get; set; }

        /// <summary>
        /// Values stored here will exist only for the lifetime of the workflow execution context.
        /// </summary>
        public Variables TransientState { get; set; } = new();

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

        public string? CorrelationId
        {
            get => WorkflowInstance.CorrelationId;
            set => WorkflowInstance.CorrelationId = value;
        }

        public bool DeleteCompletedInstances => WorkflowBlueprint.DeleteCompletedInstances;
        public ICollection<string> ExecutionLog => new List<string>();
        public WorkflowStatus Status => WorkflowInstance.WorkflowStatus;
        public bool HasBlockingActivities => WorkflowInstance.BlockingActivities.Any();
        public object? WorkflowContext { get; set; }

        public void SetVariable(string name, object? value) => WorkflowInstance.Variables.Set(name, value);
        public T? GetVariable<T>(string name) => WorkflowInstance.Variables.Get<T>(name);
        public object? GetVariable(string name) => WorkflowInstance.Variables.Get(name);
        public void SetTransientVariable(string name, object? value) => TransientState.Set(name, value);
        public T? GetTransientVariable<T>(string name) => TransientState.Get<T>(name);
        public object? GetTransientVariable(string name) => TransientState.Get(name);
        public void CompletePass() => IsFirstPass = false;
        public void Begin() => WorkflowInstance.WorkflowStatus = WorkflowStatus.Running;
        public void Resume() => WorkflowInstance.WorkflowStatus = WorkflowStatus.Running;
        public void Suspend() => WorkflowInstance.WorkflowStatus = WorkflowStatus.Suspended;

        public void Fault(string? activityId, LocalizedString? message)
        {
            WorkflowInstance.WorkflowStatus = WorkflowStatus.Faulted;
            WorkflowFault = new WorkflowFault(activityId, message);
        }

        public void Complete() => WorkflowInstance.WorkflowStatus = WorkflowStatus.Finished;

        public IActivityBlueprint? GetActivityBlueprintById(string id) => WorkflowBlueprint.Activities.FirstOrDefault(x => x.Id == id);
        public IActivityBlueprint? GetActivityBlueprintByName(string name) => WorkflowBlueprint.Activities.FirstOrDefault(x => x.Name == name);

        public void SchedulePostActivity()
        {
            var activity = WorkflowInstance.PostScheduledActivities.Pop();
            ScheduleActivity(activity);
        }

        public object? GetOutputFrom(string activityName)
        {
            var activityBlueprint = GetActivityBlueprintByName(activityName)!;
            var activityInstance = WorkflowInstance.Activities.Single(x => x.Id == activityBlueprint.Id);
            return activityInstance.Output;
        }

        public T GetOutputFrom<T>(string activityName) => (T)GetOutputFrom(activityName)!;
        public void SetWorkflowContext(object? value) => WorkflowContext = value;
        public T GetWorkflowContext<T>() => (T)WorkflowContext!;

        public async ValueTask<IEnumerable<RuntimeActivityInstance>> ActivateActivitiesAsync(CancellationToken cancellationToken = default)
        {
            var activityExecutionContexts = WorkflowBlueprint.Activities.Select(x => new ActivityExecutionContext(ServiceScope, this, x, null, CancellationToken.None));
            return await Task.WhenAll(activityExecutionContexts.Select(async x => await x.ActivateActivityAsync(cancellationToken)));
        } 
    }
}