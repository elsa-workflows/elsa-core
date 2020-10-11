using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Services.Models
{
    public class ActivityExecutionContext
    {
        public ActivityExecutionContext(
            WorkflowExecutionContext workflowExecutionContext,
            ActivityDefinition activityDefinition,
            object? input = null)
        {
            WorkflowExecutionContext = workflowExecutionContext;
            ActivityDefinition = activityDefinition;
            Input = input;
            Outcomes = new List<string>(0);
        }

        public WorkflowExecutionContext WorkflowExecutionContext { get; }
        public ActivityDefinition ActivityDefinition { get; }
        public object? Input { get; }
        public object? Output { get; set; }
        public IReadOnlyCollection<string> Outcomes { get; set; }

        public void SetVariable(string name, object? value) => WorkflowExecutionContext.SetVariable(name, value);
        public object? GetVariable(string name) => WorkflowExecutionContext.GetVariable(name);
        public T GetVariable<T>(string name) => WorkflowExecutionContext.GetVariable<T>(name);
        public T GetService<T>() => WorkflowExecutionContext.ServiceProvider.GetService<T>();

        public async ValueTask SetActivityPropertiesAsync(IActivity activity,
            CancellationToken cancellationToken = default) =>
            await WorkflowExecutionContext.ActivityPropertyProviders.SetActivityPropertiesAsync(
                activity,
                this,
                cancellationToken);
    }
}