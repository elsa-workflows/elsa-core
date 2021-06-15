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
            var properties = persistableProperties.Where(x => x.GetCustomAttribute<ActivityInputAttribute>() != null || x.GetCustomAttribute<ActivityOutputAttribute>() != null);

            // Persist input & output properties.
            foreach (var property in properties)
            {
                var value = property.GetValue(activity);
                await SavePropertyAsync(activityExecutionContext, property.Name, value, cancellationToken);
            }
            
            // Handle "inline" activities with output.
            if (activityExecutionContext.Output != null) 
                await SavePropertyAsync(activityExecutionContext, "Output", activityExecutionContext.Output, cancellationToken);
        }

        private async Task SavePropertyAsync(ActivityExecutionContext context, string propertyName, object? value, CancellationToken cancellationToken)
        {
            var propertyStorageProviderDictionary = context.ActivityBlueprint.PropertyStorageProviders;
            var providerName = propertyStorageProviderDictionary.GetItem(propertyName);
            var workflowStorageContext = new WorkflowStorageContext(context.WorkflowInstance, context.ActivityId);

            await _workflowStorageService.SaveAsync(providerName, workflowStorageContext, propertyName, value, cancellationToken);
            
            // By convention, properties named "Output" will be stored as the workflow output.
            if (propertyName == "Output")
                context.WorkflowInstance.Output = new WorkflowOutputReference(providerName, context.ActivityId);
        }
    }
}