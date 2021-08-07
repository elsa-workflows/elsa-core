using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Services.Models
{
    public class ActivityExecutionContext
    {
        public ActivityExecutionContext(
            IServiceProvider serviceProvider,
            WorkflowExecutionContext workflowExecutionContext,
            IActivityBlueprint activityBlueprint,
            object? input,
            bool resuming,
            CancellationToken cancellationToken)
        {
            WorkflowExecutionContext = workflowExecutionContext;
            ServiceProvider = serviceProvider;
            ActivityBlueprint = activityBlueprint;
            Input = input;
            Resuming = resuming;
            CancellationToken = cancellationToken;
            Outcomes = new List<string>(0);
        }

        public WorkflowExecutionContext WorkflowExecutionContext { get; }
        public WorkflowInstance WorkflowInstance => WorkflowExecutionContext.WorkflowInstance;
        public IServiceProvider ServiceProvider { get; }
        public IActivityBlueprint ActivityBlueprint { get; }

        public string ActivityId => ActivityBlueprint.Id;

        public string? ContextId
        {
            get => WorkflowExecutionContext.ContextId;
            set => WorkflowExecutionContext.ContextId = value;
        }

        public IReadOnlyCollection<string> Outcomes { get; set; }
        public object? Input { get; }
        public bool Resuming { get; }
        public bool IsFirstPass => WorkflowExecutionContext.IsFirstPass;

        public string CorrelationId
        {
            get => WorkflowExecutionContext.CorrelationId;
            set => WorkflowExecutionContext.CorrelationId = value;
        }

        public CancellationToken CancellationToken { get; }

        public IDictionary<string, object?> GetData() => WorkflowInstance.ActivityData.GetItem(ActivityBlueprint.Id, () => new Dictionary<string, object?>());

        public void SetState(string propertyName, object? value)
        {
            var data = GetData();
            data.SetState(propertyName, value);
        }

        public T? GetState<T>(string propertyName)
        {
            var data = GetData();
            return data.GetState<T>(propertyName);
        }

        public T GetState<T>(string propertyName, Func<T> defaultValue)
        {
            var data = GetData();
            return data.GetState(propertyName, defaultValue);
        }

        public object? GetState(string propertyName, Type targetType)
        {
            var data = GetData();
            return data.GetState(propertyName, targetType);
        }


        public T? GetState<TActivity, T>(Expression<Func<TActivity, T>> propertyExpression) where TActivity : IActivity
        {
            var expression = (MemberExpression) propertyExpression.Body;
            string propertyName = expression.Member.Name;
            return GetState<T>(propertyName);
        }

        public object? GetState(string propertyName)
        {
            var data = GetData();
            return data.GetState(propertyName);
        }

        public T? GetContainerState<T>() => GetContainerState<T>(typeof(T).Name);

        public T? GetContainerState<T>(string key)
        {
            var parentActivityId = ActivityBlueprint.Parent?.Id;

            if (parentActivityId == null)
                return default;

            var parentData = WorkflowExecutionContext.WorkflowInstance.ActivityData.GetItem(parentActivityId);
            return parentData.GetState<T>(key);
        }

        public void SetContainerState<T>(object? value) => SetContainerState(typeof(T).Name, value);

        public void SetContainerState(string key, object? value)
        {
            var parentActivityId = ActivityBlueprint.Parent?.Id;

            if (parentActivityId == null)
                return;

            var parentData = WorkflowExecutionContext.WorkflowInstance.ActivityData.GetItem(parentActivityId);
            parentData?.SetState(key, value);
        }

        public ActivityScope CreateScope() => WorkflowExecutionContext.CreateScope(ActivityId);
        public ActivityScope? CurrentScope => WorkflowExecutionContext.CurrentScope;
        public object? Output { get; set; }

        public ActivityScope GetScope(string activityId) => WorkflowExecutionContext.GetScope(activityId);
        public ActivityScope GetNamedScope(string activityName) => WorkflowExecutionContext.GetNamedScope(activityName);

        public void SetVariable(string name, object? value) => WorkflowExecutionContext.SetVariable(name, value);

        public T? SetVariable<T>(string name, Func<T?, T?> updater)
        {
            var value = GetVariable<T>(name);
            value = updater(value);
            SetVariable(name, value);
            return value;
        }

        public object? GetVariable(string name) => WorkflowExecutionContext.GetVariable(name);
        public T? GetVariable<T>(string name) => WorkflowExecutionContext.GetVariable<T>(name);
        public T? GetVariable<T>() => GetVariable<T>(typeof(T).Name);
        public bool HasVariable(string name) => WorkflowExecutionContext.HasVariable(name);

        /// <summary>
        /// Clears all of the variables associated with the current <see cref="Elsa.Models.WorkflowInstance"/>.
        /// </summary>
        /// <seealso cref="WorkflowExecutionContext.PurgeVariables"/>
        /// <seealso cref="Variables.RemoveAll"/>
        public void PurgeVariables() => WorkflowExecutionContext.PurgeVariables();

        public void SetTransientVariable(string name, object? value) => WorkflowExecutionContext.SetTransientVariable(name, value);
        public object? GetTransientVariable(string name) => WorkflowExecutionContext.GetTransientVariable(name);
        public T? GetTransientVariable<T>(string name) => WorkflowExecutionContext.GetTransientVariable<T>(name);
        public T? GetTransientVariable<T>() => GetTransientVariable<T>(typeof(T).Name);
        public T GetService<T>() where T : notnull => ServiceProvider.GetRequiredService<T>();

        public async ValueTask<RuntimeActivityInstance> ActivateActivityAsync(CancellationToken cancellationToken = default)
        {
            var activityTypeService = ServiceProvider.GetRequiredService<IActivityTypeService>();
            return await activityTypeService.ActivateActivityAsync(ActivityBlueprint, cancellationToken);
        }

        public T? GetInput<T>() => Input.ConvertTo<T>();
        public T? GetInput<T>(Func<T?> defaultValue) => Input != null ? Input.ConvertTo<T>() : defaultValue();
        public T? GetInput<T>(T? defaultValue) => Input != null ? Input.ConvertTo<T>() : defaultValue;

        public Task<object?> GetNamedActivityPropertyAsync(string activityName, string propertyName, CancellationToken cancellationToken = default) => WorkflowExecutionContext.GetNamedActivityPropertyAsync(GetCompositeName(activityName), propertyName, cancellationToken);
        public async Task<object?> GetNamedActivityPropertyAsync(string activityName, string propertyName, object? defaultValue, CancellationToken cancellationToken = default) => await WorkflowExecutionContext.GetNamedActivityPropertyAsync(GetCompositeName(activityName), propertyName, cancellationToken) ?? defaultValue;
        public async Task<object?> GetNamedActivityPropertyAsync(string activityName, string propertyName, Func<object?> defaultValue, CancellationToken cancellationToken = default) => await WorkflowExecutionContext.GetNamedActivityPropertyAsync(GetCompositeName(activityName), propertyName, cancellationToken) ?? defaultValue();
        public async Task<T?> GetNamedActivityPropertyAsync<T>(string activityName, string propertyName, CancellationToken cancellationToken = default) => await WorkflowExecutionContext.GetNamedActivityPropertyAsync<T>(GetCompositeName(activityName), propertyName, cancellationToken);
        public async Task<T?> GetNamedActivityPropertyAsync<T>(string activityName, string propertyName, Func<T?> defaultValue, CancellationToken cancellationToken = default) => await GetNamedActivityPropertyAsync<T>(GetCompositeName(activityName), propertyName, cancellationToken) ?? defaultValue();
        public async Task<T?> GetNamedActivityPropertyAsync<T>(string activityName, string propertyName, T? defaultValue) => await GetNamedActivityPropertyAsync(GetCompositeName(activityName), propertyName, () => defaultValue, CancellationToken);

        public async Task<T?> GetNamedActivityPropertyAsync<TActivity, T>(string activityName, Expression<Func<TActivity, T>> propertyExpression, CancellationToken cancellationToken = default) where TActivity : IActivity => await WorkflowExecutionContext.GetNamedActivityPropertyAsync(GetCompositeName(activityName), propertyExpression, cancellationToken);
        public async Task<T?> GetNamedActivityPropertyAsync<TActivity, T>(string activityName, Expression<Func<TActivity, T>> propertyExpression, Func<T?> defaultValue, CancellationToken cancellationToken = default) where TActivity : IActivity => await GetNamedActivityPropertyAsync(GetCompositeName(activityName), propertyExpression, cancellationToken) ?? defaultValue();
        public async Task<T?> GetNamedActivityPropertyAsync<TActivity, T>(string activityName, Expression<Func<TActivity, T>> propertyExpression, T? defaultValue) where TActivity : IActivity => await GetNamedActivityPropertyAsync(GetCompositeName(activityName), propertyExpression, () => defaultValue, CancellationToken);

        public void SetWorkflowContext(object? value) => WorkflowExecutionContext.SetWorkflowContext(value);
        public object? GetWorkflowContext() => WorkflowExecutionContext.GetWorkflowContext();
        public T GetWorkflowContext<T>() => WorkflowExecutionContext.GetWorkflowContext<T>();
        public IDictionary<string, object?> GetActivityData() => GetActivityData(ActivityId);
        public IDictionary<string, object?> GetActivityData(string activityId) => WorkflowExecutionContext.GetActivityData(activityId);
        public Task<T?> GetActivityPropertyAsync<TActivity, T>(Expression<Func<TActivity, T>> propertyExpression, CancellationToken cancellationToken = default) where TActivity : IActivity => WorkflowExecutionContext.GetActivityPropertyAsync(ActivityId, propertyExpression, cancellationToken);
        public void Fault(Exception exception) => WorkflowExecutionContext.Fault(exception, ActivityId, Input, Resuming);

        private ICompositeActivityBlueprint GetContainerActivity()
        {
            var current = ActivityBlueprint;

            while (current is not ICompositeActivityBlueprint)
                current = current.Parent;

            return (ICompositeActivityBlueprint)current;
        }

        private string GetCompositeName(string activityName)
        {
            var container = GetContainerActivity();
            return container.Parent == null ? activityName : $"{container.Id}:{activityName}";
        }
    }
}