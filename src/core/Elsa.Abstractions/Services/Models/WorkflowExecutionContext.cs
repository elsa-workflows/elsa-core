using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Models;
using Elsa.Providers.WorkflowStorage;
using Elsa.Services.WorkflowStorage;
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

        public void ScheduleActivities(IEnumerable<string> activityIds, object? input = null)
        {
            foreach (var activityId in activityIds)
                ScheduleActivity(activityId, input);
        }

        public void ScheduleActivities(IEnumerable<ScheduledActivity> activities, object? input = null)
        {
            foreach (var activity in activities)
                ScheduleActivity(activity);
        }

        public void ScheduleActivity(string activityId, object? input = null) => ScheduleActivity(new ScheduledActivity(activityId, input));
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
        /// A collection of tasks to execute after the workflow is suspended.
        /// This is useful for avoiding race conditions, such as sending a message and then waiting for a message to be received using some MessageReceived activity for example. If the workflow didn't get suspended while that message is received, the workflow would get stuck.
        /// By designing activities such as `SendMessage` to only actually send the message post-suspension using the Tasks collection, you can be sure that the workflow gets suspended and blocked on the `MessageReceived` activity before a message reply gets received.  
        /// </summary>
        public ICollection<Func<WorkflowExecutionContext, CancellationToken, ValueTask>> Tasks { get; private set; } = new List<Func<WorkflowExecutionContext, CancellationToken, ValueTask>>();

        public string? ContextId
        {
            get => WorkflowInstance.ContextId;
            set => WorkflowInstance.ContextId = value;
        }

        public void RegisterTask(Func<WorkflowExecutionContext, CancellationToken, ValueTask> task) => Tasks.Add(task);

        public async ValueTask ProcessRegisteredTasksAsync(CancellationToken cancellationToken = default)
        {
            var tasks = Tasks.ToList();
            Tasks = new List<Func<WorkflowExecutionContext, CancellationToken, ValueTask>>();

            foreach (var task in tasks)
                await task(this, cancellationToken);
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
        public bool HasVariable(string name) => WorkflowInstance.Variables.Has(name);

        /// <summary>
        /// Gets a variable from across all scopes, starting with the current scope, going up each scope until the requested variable is found.
        /// </summary>
        /// <remarks>Use <see cref="GetWorkflowVariable"/>if you want to access a workflow variable directly without going through the scopes.</remarks>
        public object? GetVariable(string name)
        {
            var mergedVariables = GetMergedVariables();
            return mergedVariables.Get(name);
        }
        
        /// <summary>
        /// Gets all variables merged across all scopes.
        /// </summary>
        public Variables GetMergedVariables()
        {
            var scopes = WorkflowInstance.Scopes.ToList();

            var mergedVariables = scopes.Select(x => x.Variables).Aggregate(WorkflowInstance.Variables, (current, next) =>
            {
                var combined = current.Data.MergedWith(next.Data);
                return new Variables(combined);
            });

            return mergedVariables;
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


        public ActivityScope? CurrentScope => WorkflowInstance.Scopes.Any() ? WorkflowInstance.Scopes.Peek() : default;
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

        public async Task<object?> GetNamedActivityPropertyAsync(string activityName, string propertyName, CancellationToken cancellationToken = default)
        {
            var activityBlueprint = GetActivityBlueprintByName(activityName)!;
            return await GetActivityPropertyAsync(activityBlueprint, propertyName, cancellationToken);
        }

        public async ValueTask<T?> GetNamedActivityPropertyAsync<T>(string activityName, string propertyName, CancellationToken cancellationToken = default) => (T?) await GetNamedActivityPropertyAsync(activityName, propertyName, cancellationToken);
        
        public async Task<T?> GetNamedActivityPropertyAsync<TActivity, T>(string activityName, Expression<Func<TActivity, T>> propertyExpression, CancellationToken cancellationToken = default) where TActivity : IActivity
        {
            var expression = (MemberExpression) propertyExpression.Body;
            string propertyName = expression.Member.Name;
            return await GetNamedActivityPropertyAsync<T>(activityName, propertyName, cancellationToken);
        }
        
        public async Task<T?> GetActivityPropertyAsync<TActivity, T>(string activityId, Expression<Func<TActivity, T>> propertyExpression, CancellationToken cancellationToken = default) where TActivity : IActivity
        {
            var expression = (MemberExpression) propertyExpression.Body;
            string propertyName = expression.Member.Name;
            return await GetActivityPropertyAsync<T>(activityId, propertyName, cancellationToken);
        }
        
        public async Task<T?> GetActivityPropertyAsync<T>(string activityId, string propertyName, CancellationToken cancellationToken = default)
        {
            var activityBlueprint = GetActivityBlueprintById(activityId)!;
            return await GetActivityPropertyAsync<T>(activityBlueprint, propertyName, cancellationToken);
        }
        
        public async Task<T?> GetActivityPropertyAsync<T>(IActivityBlueprint activityBlueprint, string propertyName, CancellationToken cancellationToken = default) => (T?)await GetActivityPropertyAsync(activityBlueprint, propertyName, cancellationToken);

        public async Task<object?> GetActivityPropertyAsync(IActivityBlueprint activityBlueprint, string propertyName, CancellationToken cancellationToken = default)
        {
            var workflowStorageService = ServiceProvider.GetRequiredService<IWorkflowStorageService>();
            var storageProviderName = activityBlueprint.PropertyStorageProviders.GetItem(propertyName);
            var context = new WorkflowStorageContext(WorkflowInstance, activityBlueprint.Id);
            return await workflowStorageService.LoadAsync(storageProviderName, context, propertyName, cancellationToken);
        }
        
        public void SetWorkflowContext(object? value) => WorkflowContext = value;
        public object? GetWorkflowContext() => WorkflowContext;
        public T GetWorkflowContext<T>() => (T) WorkflowContext!;
        
        public IDictionary<string, object> GetActivityData(string activityId)
        {
            var activityData = WorkflowInstance.ActivityData;
            var state = activityData.ContainsKey(activityId) ? activityData[activityId] : default;

            if (state != null) 
                return state;
            
            state = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            activityData[activityId] = state;

            return state;
        }

        private async Task EvictScopeAsync(ActivityScope scope)
        {
            var scopeActivity = WorkflowBlueprint.GetActivity(scope.ActivityId)!;
            await EvictScopeAsync(scopeActivity);
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