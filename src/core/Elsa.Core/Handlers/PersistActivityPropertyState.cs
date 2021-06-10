using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Events;
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
            var persistableProperties = activityType.GetProperties().Where(x => x.GetCustomAttribute<NonPersistableAttribute>() == null).ToList();
            var properties = persistableProperties.Where(x => x.GetCustomAttribute<ActivityInputAttribute>() != null || x.GetCustomAttribute<ActivityOutputAttribute>() != null);
            var activityExecutionContext = notification.ActivityExecutionContext;
            var propertyStorageProviderDictionary = activityExecutionContext.ActivityBlueprint.PropertyStorageProviders;
            
            // Persist input & output properties.
            foreach (var property in properties)
            {
                var value = property.GetValue(activity);
                await SavePropertyAsync(activityExecutionContext, property.Name, value, cancellationToken);
            }
            
            // Persist special "output" property.
            await SavePropertyAsync(activityExecutionContext, ActivityOutput.PropertyName, activityExecutionContext.Output, cancellationToken);
        }

        private async Task SavePropertyAsync(ActivityExecutionContext context, string propertyName, object? value, CancellationToken cancellationToken)
        {
            var propertyStorageProviderDictionary = context.ActivityBlueprint.PropertyStorageProviders;
            var providerName = propertyStorageProviderDictionary.GetItem(propertyName);
            var provider = _workflowStorageService.GetProviderByNameOrDefault(providerName);

            await provider.SaveAsync(context, propertyName, value, cancellationToken);
        }
    }
}