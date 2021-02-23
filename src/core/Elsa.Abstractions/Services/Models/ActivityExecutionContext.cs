﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Elsa.ActivityProviders;
using Elsa.Models;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

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
        
        public string? CorrelationId
        {
            get => WorkflowExecutionContext.CorrelationId;
            set => WorkflowExecutionContext.CorrelationId = value;
        }
        
        public CancellationToken CancellationToken { get; }

        public JObject GetData() => WorkflowInstance.ActivityData.GetItem(ActivityBlueprint.Id, () => new JObject());

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
        
        public T? GetState<TActivity, T>(Expression<Func<TActivity, T>> propertyExpression) where TActivity : IActivity
        {
            var expression = (MemberExpression) propertyExpression.Body;
            string propertyName = expression.Member.Name;
            return GetState<T>(propertyName);
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

        public object? Output
        {
            get => WorkflowExecutionContext.WorkflowInstance.ActivityOutput.GetItem(ActivityBlueprint.Id, () => null!);
            set => WorkflowExecutionContext.WorkflowInstance.ActivityOutput.SetItem(ActivityBlueprint.Id, value);
        }

        public ActivityScope CurrentScope => WorkflowExecutionContext.CurrentScope;
        public ActivityScope GetScope(string activityId) => WorkflowExecutionContext.GetScope(activityId);
        public ActivityScope GetNamedScope(string activityName) => WorkflowExecutionContext.GetNamedScope(activityName);

        public void SetVariable(string name, object? value) => WorkflowExecutionContext.SetVariable(name, value);
        public object? GetVariable(string name) => WorkflowExecutionContext.GetVariable(name);
        public T? GetVariable<T>(string name) => WorkflowExecutionContext.GetVariable<T>(name);
        public T? GetVariable<T>() => GetVariable<T>(typeof(T).Name);

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

        public object? GetOutputFrom(string activityName) => WorkflowExecutionContext.GetOutputFrom(activityName);
        public object? GetOutputFrom(string activityName, object? defaultValue) => WorkflowExecutionContext.GetOutputFrom(activityName) ?? defaultValue;
        public object? GetOutputFrom(string activityName, Func<object?> defaultValue) => WorkflowExecutionContext.GetOutputFrom(activityName) ?? defaultValue();
        public T? GetOutputFrom<T>(string activityName) => (T?) GetOutputFrom(activityName)!;
        public T? GetOutputFrom<T>(string activityName, Func<T?> defaultValue) => (T?) GetOutputFrom(activityName, defaultValue())!;
        public T? GetOutputFrom<T>(string activityName, T? defaultValue) => (T?) GetOutputFrom(activityName, () => defaultValue)!;
        public void SetWorkflowContext(object? value) => WorkflowExecutionContext.SetWorkflowContext(value);
        public T GetWorkflowContext<T>() => WorkflowExecutionContext.GetWorkflowContext<T>();
    }
}