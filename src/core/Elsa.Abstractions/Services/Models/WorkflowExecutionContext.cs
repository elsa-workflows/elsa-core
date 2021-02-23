using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Models;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NodaTime;
using Rebus.Extensions;

namespace Elsa.Services.Models
{
    public class WorkflowExecutionContext
    {
        public WorkflowExecutionContext(
            IServiceProvider serviceProvider,
            IWorkflowBlueprint workflowBlueprint,
            WorkflowInstance workflowInstance,
            object? input = default
        )
        {
            ServiceProvider = serviceProvider;
            WorkflowBlueprint = workflowBlueprint;
            WorkflowInstance = workflowInstance;
            Input = input;
            IsFirstPass = true;
            Serializer = serviceProvider.GetRequiredService<JsonSerializer>();
            Mediator = serviceProvider.GetRequiredService<IMediator>();
        }

        public IWorkflowBlueprint WorkflowBlueprint { get; }
        public IServiceProvider ServiceProvider { get; }
        public WorkflowInstance WorkflowInstance { get; }
        public JsonSerializer Serializer { get; }
        public IMediator Mediator { get; }
        public object? Input { get; }
        public bool HasScheduledActivities => WorkflowInstance.ScheduledActivities.Any();
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

        public void ScheduleActivity(string activityId, object? input = default) => ScheduleActivity(new ScheduledActivity(activityId, input));
        public void ScheduleActivity(ScheduledActivity activity) => WorkflowInstance.ScheduledActivities.Push(activity);
        public ScheduledActivity PopScheduledActivity() => WorkflowInstance.ScheduledActivities.Pop();
        public ScheduledActivity PeekScheduledActivity() => WorkflowInstance.ScheduledActivities.Peek();

        public string? CorrelationId
        {
            get => WorkflowInstance.CorrelationId;
            set => WorkflowInstance.CorrelationId = value;
        }

        public bool DeleteCompletedInstances => WorkflowBlueprint.DeleteCompletedInstances;
        public IList<IConnection> ExecutionLog { get; } = new List<IConnection>();
        public WorkflowStatus Status => WorkflowInstance.WorkflowStatus;
        public bool HasBlockingActivities => WorkflowInstance.BlockingActivities.Any();
        public object? WorkflowContext { get; set; }

        /// <summary>
        /// A collection of tasks to execute post-workflow burst execution.
        /// </summary>
        public IDictionary<string, ICollection<Func<WorkflowExecutionContext, CancellationToken, ValueTask>>> Tasks { get; set; } = new Dictionary<string, ICollection<Func<WorkflowExecutionContext, CancellationToken, ValueTask>>>();

        public string? ContextId
        {
            get => WorkflowInstance.ContextId;
            set => WorkflowInstance.ContextId = value;
        }

        public void RegisterTask(string groupName, Func<WorkflowExecutionContext, CancellationToken, ValueTask> task)
        {
            if (!Tasks.ContainsKey(groupName))
            {
                var list = new List<Func<WorkflowExecutionContext, CancellationToken, ValueTask>> { task };
                Tasks[groupName] = list;
            }
            else
            {
                Tasks[groupName].Add(task);
            }
        }

        public IEnumerable<Func<WorkflowExecutionContext, CancellationToken, ValueTask>> GetRegisteredTasks(string groupName) =>
            Tasks.ContainsKey(groupName) ? Tasks[groupName] : Enumerable.Empty<Func<WorkflowExecutionContext, CancellationToken, ValueTask>>();

        public async ValueTask ExecuteRegisteredTasksAsync(string groupName, CancellationToken cancellationToken = default)
        {
            var tasks = GetRegisteredTasks(groupName);

            foreach (var task in tasks)
                await task(this, cancellationToken);

            Tasks.Remove(groupName);
        }

        public async Task RemoveBlockingActivityAsync(BlockingActivity blockingActivity)
        {
            WorkflowInstance.BlockingActivities.Remove(blockingActivity);
            await Mediator.Publish(new BlockingActivityRemoved(this, blockingActivity));
        }

        public async Task EvictScopeAsync(IActivityBlueprint scope) => await Mediator.Publish(new ScopeEvicted(this, scope));

        public void SetVariable(string name, object? value) => WorkflowInstance.Variables.Set(name, value);
        public T? GetVariable<T>() => GetVariable<T>(typeof(T).Name);
        public T? GetVariable<T>(string name) => WorkflowInstance.Variables.Get<T>(name);
        
        /// <summary>
        /// Gets a variable from across all scopes, starting with the current scope, going up each scope until the requested variable is found.
        /// </summary>
        /// <remarks>Use <see cref="GetWorkflowVariable"/>if you want to access a workflow variable directly without going through the scopes.</remarks>
        public object? GetVariable(string name)
        {
            var scopes = WorkflowInstance.Scopes.ToList();
            
            var mergedVariables = scopes.Select(x => x.Variables).Aggregate(new Variables(), (current, next) =>
            {
                var combined = current.Data.MergedWith(next.Data);
                return new Variables(combined);
            });
            
            return mergedVariables.Get(name);
        }
        
        /// <summary>
        /// Gets a workflow variable.
        /// </summary>
        public object? GetWorkflowVariable(string name) => WorkflowInstance.Variables.Get(name);

        /// <summary>
        /// Clears all of the variables associated with the current <see cref="WorkflowInstance"/>.
        /// </summary>
        /// <seealso cref="Variables.RemoveAll"/>
        public void PurgeVariables() => WorkflowInstance.Variables.RemoveAll();


        public ActivityScope CurrentScope => WorkflowInstance.Scopes.Peek();
        public ActivityScope GetScope(string activityId) => WorkflowInstance.Scopes.First(x => x.ActivityId == activityId);
        
        public ActivityScope GetNamedScope(string activityName)
        {
            var activityBlueprint = GetActivityBlueprintByName(activityName)!;
            return GetScope(activityBlueprint.Id);
        }

        public void SetTransientVariable(string name, object? value) => TransientState.Set(name, value);
        public T? GetTransientVariable<T>(string name) => TransientState.Get<T>(name);
        public object? GetTransientVariable(string name) => TransientState.Get(name);
        public void CompletePass() => IsFirstPass = false;
        public void Begin() => WorkflowInstance.WorkflowStatus = WorkflowStatus.Running;
        public void Resume() => WorkflowInstance.WorkflowStatus = WorkflowStatus.Running;
        public void Suspend() => WorkflowInstance.WorkflowStatus = WorkflowStatus.Suspended;

        public void Fault(Exception ex, string? activityId, object? activityInput, bool resuming) => Fault(ex, ex.Message, activityId, activityInput, resuming);
        public void Fault(string message, string? activityId, object? activityInput, bool resuming) => Fault(null, message, activityId, activityInput, resuming);

        public void Fault(Exception? exception, string message, string? activityId, object? activityInput, bool resuming)
        {
            var clock = ServiceProvider.GetRequiredService<IClock>();
            WorkflowInstance.WorkflowStatus = WorkflowStatus.Faulted;
            WorkflowInstance.FaultedAt = clock.GetCurrentInstant();
            WorkflowInstance.Fault = new WorkflowFault(SimpleException.FromException(exception), message, activityId, activityInput, resuming);
        }

        public async Task CompleteAsync()
        {
            // Remove all blocking activities.
            foreach (var blockingActivity in WorkflowInstance.BlockingActivities)
                await RemoveBlockingActivityAsync(blockingActivity);

            // Evict all scopes.
            foreach (var scope in WorkflowInstance.Scopes.AsEnumerable().Reverse())
                await EvictScopeAsync(scope);

            WorkflowInstance.Scopes = new SimpleStack<ActivityScope>();
            WorkflowInstance.WorkflowStatus = WorkflowStatus.Finished;
        }

        public IActivityBlueprint? GetActivityBlueprintById(string id) => WorkflowBlueprint.Activities.FirstOrDefault(x => x.Id == id);
        public IActivityBlueprint? GetActivityBlueprintByName(string name) => WorkflowBlueprint.Activities.FirstOrDefault(x => x.Name == name);

        public object? GetOutputFrom(string activityName)
        {
            var activityBlueprint = GetActivityBlueprintByName(activityName)!;
            return WorkflowInstance.ActivityOutput.GetItem(activityBlueprint.Id);
        }

        public T GetOutputFrom<T>(string activityName) => (T) GetOutputFrom(activityName)!;
        public void SetWorkflowContext(object? value) => WorkflowContext = value;
        public T GetWorkflowContext<T>() => (T) WorkflowContext!;

        /// <summary>
        /// Remove empty activity data to save on document size.
        /// </summary>
        internal void PruneActivityData()
        {
            WorkflowInstance.ActivityData.Prune(x => x.Value.Count == 0);
            WorkflowInstance.ActivityOutput.Prune(x => x.Value == null || !ShouldPersistActivityOutput(x.Key));
        }

        private bool ShouldPersistActivityOutput(string activityId)
        {
            var activityBlueprint = WorkflowBlueprint.GetActivity(activityId);
            return activityBlueprint != null && activityBlueprint.PersistOutput;
        }

        private async Task<IActivityBlueprint> EvictScopeAsync(ActivityScope scope)
        {
            var scopeActivity = WorkflowBlueprint.GetActivity(scope.ActivityId)!;
            await EvictScopeAsync(scopeActivity);
            return scopeActivity;
        }

        public ActivityScope CreateScope(string activityId)
        {
            var scope = new ActivityScope(activityId);
            WorkflowInstance.Scopes.Push(scope);
            return scope;
        }
    }

    public record ExecutionLogEntry(string ActivityId, string Outcome);
}