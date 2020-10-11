using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Services.Models
{
    public class ActivityExecutionContext
    {
        public ActivityExecutionContext(
            WorkflowExecutionContext workflowExecutionContext,
            IActivity activity,
            object? input = null)
        {
            WorkflowExecutionContext = workflowExecutionContext;
            Activity = activity;
            Input = input;
            Outcomes = new List<string>(0);
        }

        public WorkflowExecutionContext WorkflowExecutionContext { get; }
        public IActivity Activity { get; }
        public object? Input { get; }
        public object? Output { get; set; }
        public IReadOnlyCollection<string> Outcomes { get; set; }

        public void SetVariable(string name, object? value) => WorkflowExecutionContext.SetVariable(name, value);
        public object? GetVariable(string name) => WorkflowExecutionContext.GetVariable(name);
        public T GetVariable<T>(string name) => WorkflowExecutionContext.GetVariable<T>(name);
        public T GetService<T>() => WorkflowExecutionContext.ServiceProvider.GetService<T>();

        public async ValueTask SetActivityPropertiesAsync(CancellationToken cancellationToken = default)
        {
            var properties = Activity.GetType().GetProperties().Where(IsActivityProperty).ToList();
            var activityPropertyValueProviders = WorkflowExecutionContext.ActivityPropertyValueProviders;
            var propertyValueProvider = activityPropertyValueProviders[Activity.Id];

            foreach (var property in properties)
            {
                if(propertyValueProvider == null || !propertyValueProvider.ContainsKey(property.Name))
                    continue;
                
                var provider = propertyValueProvider[property.Name];
                var value = await provider.GetValueAsync( this, cancellationToken);
                property.SetValue(Activity, value);
            }
        }

        private bool IsActivityProperty(PropertyInfo property) =>
            property.GetCustomAttribute<ActivityPropertyAttribute>() != null;
    }
}