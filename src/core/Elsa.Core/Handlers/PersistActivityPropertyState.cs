using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Events;
using Elsa.Providers.WorkflowStorage;
using Elsa.Services.Models;
using Elsa.Services.WorkflowStorage;
using MediatR;

namespace Elsa.Handlers
{
    /// <summary>
    /// Persists runtime activity input & output property values.  
    /// </summary>
    public class PersistActivityPropertyState : INotificationHandler<ActivityExecuted>
    {
        private readonly IWorkflowStorageService _workflowStorageService;

        public PersistActivityPropertyState(IWorkflowStorageService workflowStorageService)
        {
            _workflowStorageService = workflowStorageService;
        }
        
        public async Task Handle(ActivityExecuted notification, CancellationToken cancellationToken)
        {
            var activity = notification.Activity;
            var activityType = activity.GetType();
            var activityExecutionContext = notification.ActivityExecutionContext;
            var persistableProperties = activityType.GetProperties().Where(x => x.GetCustomAttribute<NonPersistableAttribute>() == null).ToList();
            var inputProperties = persistableProperties.Where(x => x.GetCustomAttribute<ActivityInputAttribute>() != null);
            var outputProperties = persistableProperties.Where(x => x.GetCustomAttribute<ActivityOutputAttribute>() != null);

            // Persist input properties.
            foreach (var property in inputProperties)
            {
                var value = property.GetValue(activity);
                var inputAttr = property.GetCustomAttribute<ActivityInputAttribute>();
                var defaultProviderName = inputAttr.DefaultWorkflowStorageProvider; 
                await SavePropertyAsync(activityExecutionContext, property.Name, value, defaultProviderName, cancellationToken);
            }
            
            // Persist output properties.
            foreach (var property in outputProperties)
            {
                var value = property.GetValue(activity);
                var outputAttr = property.GetCustomAttribute<ActivityOutputAttribute>();
                var defaultProviderName = outputAttr.DefaultWorkflowStorageProvider; 
                await SavePropertyAsync(activityExecutionContext, property.Name, value, defaultProviderName, cancellationToken);
            }
            
            // Handle "inline" activities with output.
            if (activityExecutionContext.Output != null) 
                await SavePropertyAsync(activityExecutionContext, "Output", activityExecutionContext.Output, default, cancellationToken);
        }

        private async Task SavePropertyAsync(ActivityExecutionContext context, string propertyName, object? value, string? defaultProviderName, CancellationToken cancellationToken)
        {
            var propertyStorageProviderDictionary = context.ActivityBlueprint.PropertyStorageProviders;
            var providerName = propertyStorageProviderDictionary.GetItem(propertyName) ?? defaultProviderName;
            var workflowStorageContext = new WorkflowStorageContext(context.WorkflowInstance, context.ActivityId);

            await _workflowStorageService.SaveAsync(providerName, workflowStorageContext, propertyName, value, cancellationToken);
            
            // By convention, properties named "Output" will be stored as the workflow output.
            if (propertyName == "Output")
                context.WorkflowInstance.Output = new WorkflowOutputReference(providerName, context.ActivityId);
        }
    }
}