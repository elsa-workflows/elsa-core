using System.Collections.Generic;
using System.Linq;
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
            IServiceScope serviceProvider,
            WorkflowExecutionContext workflowExecutionContext,
            IActivityBlueprint activityBlueprint,
            object? input,
            CancellationToken cancellationToken)
        {
            WorkflowExecutionContext = workflowExecutionContext;
            ServiceScope = serviceProvider;
            ActivityBlueprint = activityBlueprint;
            ActivityInstance = WorkflowExecutionContext.WorkflowInstance.Activities.First(x => x.Id == ActivityBlueprint.Id);
            Input = input;
            CancellationToken = cancellationToken;
            Outcomes = new List<string>(0);
        }

        public WorkflowExecutionContext WorkflowExecutionContext { get; }
        public IServiceScope ServiceScope { get; }
        public IActivityBlueprint ActivityBlueprint { get; }
        public ActivityInstance ActivityInstance { get; }
        public IReadOnlyCollection<string> Outcomes { get; set; }
        public object? Input { get; }
        public CancellationToken CancellationToken { get; }
        public JObject Data => ActivityInstance.Data;

        public object? Output
        {
            get => ActivityInstance.Output;
            set => ActivityInstance.Output = value;
        }

        public void SetVariable(string name, object? value) => WorkflowExecutionContext.SetVariable(name, value);
        public object? GetVariable(string name) => WorkflowExecutionContext.GetVariable(name);
        public T GetVariable<T>(string name) => WorkflowExecutionContext.GetVariable<T>(name);
        public T GetVariable<T>() => GetVariable<T>(typeof(T).Name);
        public T GetService<T>() where T : notnull => ServiceScope.ServiceProvider.GetRequiredService<T>();

        public async ValueTask<RuntimeActivityInstance> ActivateActivityAsync(CancellationToken cancellationToken = default)
        {
            var activityTypeService = ServiceScope.ServiceProvider.GetRequiredService<IActivityTypeService>();
            return await activityTypeService.ActivateActivityAsync(ActivityBlueprint, cancellationToken);
        }

        public T GetInput<T>() => (T) Input!;
        public object? GetOutputFrom(string activityName) => WorkflowExecutionContext.GetOutputFrom(activityName);
        public T GetOutputFrom<T>(string activityName) => (T) GetOutputFrom(activityName)!;
        public void SetWorkflowContext(object? value) => WorkflowExecutionContext.SetWorkflowContext(value);
        public T GetWorkflowContext<T>() => WorkflowExecutionContext.GetWorkflowContext<T>();
    }
}