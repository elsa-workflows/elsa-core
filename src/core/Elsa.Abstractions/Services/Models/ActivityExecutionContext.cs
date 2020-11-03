using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Services.Models
{
    public class ActivityExecutionContext
    {
        public ActivityExecutionContext(
            WorkflowExecutionContext workflowExecutionContext,
            IServiceProvider serviceProvider,
            IActivityBlueprint activityBlueprint,
            object? input = null)
        {
            WorkflowExecutionContext = workflowExecutionContext;
            ServiceProvider = serviceProvider;
            ActivityBlueprint = activityBlueprint;
            ActivityInstance = WorkflowExecutionContext.WorkflowInstance.Activities.First(x => x.Id == ActivityBlueprint.Id);
            Input = input;
            Outcomes = new List<string>(0);
        }

        public WorkflowExecutionContext WorkflowExecutionContext { get; }
        public IServiceProvider ServiceProvider { get; }
        public IActivityBlueprint ActivityBlueprint { get; }
        public ActivityInstance ActivityInstance { get; }
        public IReadOnlyCollection<string> Outcomes { get; set; }
        public object? Input { get; }

        public object? Output
        {
            get => ActivityInstance.Output;
            set => ActivityInstance.Output = value;
        }

        public void SetVariable(string name, object? value) => WorkflowExecutionContext.SetVariable(name, value);
        public object? GetVariable(string name) => WorkflowExecutionContext.GetVariable(name);
        public T GetVariable<T>(string name) => WorkflowExecutionContext.GetVariable<T>(name);
        public T GetVariable<T>() => GetVariable<T>(typeof(T).Name);
        public T GetService<T>() => WorkflowExecutionContext.ServiceProvider.GetService<T>();

        public async ValueTask SetActivityPropertiesAsync(
            IActivity activity,
            CancellationToken cancellationToken = default) =>
            await WorkflowExecutionContext.WorkflowBlueprint.ActivityPropertyProviders.SetActivityPropertiesAsync(
                activity,
                this,
                cancellationToken);

        public IActivity ActivateActivity(string activityType, Action<IActivity>? setupActivity = default)
        {
            var activityActivator = ServiceProvider.GetRequiredService<IActivityActivator>();
            var activity = activityActivator.ActivateActivity(activityType, setupActivity);
            activity.Data = ActivityInstance.Data;
            return activity;
        }

        public T GetInput<T>() => (T)Input!; 
        public object? GetOutputFrom(string activityName) => WorkflowExecutionContext.GetOutputFrom(activityName);
        public T GetOutputFrom<T>(string activityName) => (T)GetOutputFrom(activityName)!;
        public void SetWorkflowContext(object? value) => WorkflowExecutionContext.SetWorkflowContext(value);
        public T GetWorkflowContext<T>() => WorkflowExecutionContext.GetWorkflowContext<T>();
    }
}