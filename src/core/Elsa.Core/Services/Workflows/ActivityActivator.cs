using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.ControlFlow;
using Elsa.Attributes;
using Elsa.Events;
using Elsa.Options;
using Elsa.Providers.WorkflowStorage;
using Elsa.Services.Models;
using Elsa.Services.WorkflowStorage;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Services.Workflows
{
    public class ActivityActivator : IActivityActivator
    {
        private readonly ElsaOptions _elsaOptions;
        private readonly IWorkflowStorageService _workflowStorageService;
        private readonly IServiceProvider _serviceProvider;

        public ActivityActivator(ElsaOptions options, IWorkflowStorageService workflowStorageService, IServiceProvider serviceProvider)
        {
            _elsaOptions = options;
            _workflowStorageService = workflowStorageService;
            _serviceProvider = serviceProvider;
        }

        public async Task<IActivity> ActivateActivityAsync(ActivityExecutionContext context, Type type, CancellationToken cancellationToken = default)
        {
            var activity = _elsaOptions.ActivityFactory.CreateService(type, context.ServiceProvider);
            activity.Data = context.GetData();
            activity.Id = context.ActivityId;

            await ApplyStoredValuesAsync(context, activity, cancellationToken);

            // TODO: Make extensible / apply open/closed.
            if (ShouldSetProperties(context, activity))
            {
                // TODO: Figure out how to deal with dynamically defined properties and what it means to set values to these.
                // ActivityTypes can have dynamic properties, so they need to be able to "intercept" when values are being applied.
                // Right now, we can only set these values on properties of the IActivity implementation.
                //var activityType = await _activityTypeService.GetActivityTypeAsync(activity.Type, cancellationToken);
                await context.WorkflowExecutionContext.WorkflowBlueprint.ActivityPropertyProviders.SetActivityPropertiesAsync(activity, context, context.CancellationToken);
                await StoreAppliedValuesAsync(context, activity, cancellationToken);
            }

            return activity;
        }

        private async ValueTask ApplyStoredValuesAsync(ActivityExecutionContext context, IActivity activity, CancellationToken cancellationToken)
        {
            await ApplyStoredObjectValuesAsync(context, activity, cancellationToken);
        }

        private async ValueTask ApplyStoredObjectValuesAsync(ActivityExecutionContext context, object activity, CancellationToken cancellationToken, string parentName = null)
        {
            var properties = activity.GetType().GetProperties().Where(IsActivityProperty).ToList();
            var nestedProperties = activity.GetType().GetProperties().Where(IsActivityObjectProperty).ToList();
            var propertyStorageProviderDictionary = context.ActivityBlueprint.PropertyStorageProviders;
            var workflowStorageContext = new WorkflowStorageContext(context.WorkflowInstance, context.ActivityId);

            foreach (var property in properties)
            {
                var propertyName = parentName == null ? property.Name : $"{parentName}_{property.Name}";
                var attr = property.GetCustomAttributes<ActivityPropertyAttributeBase>().First();
                var providerName = propertyStorageProviderDictionary.GetItem(propertyName) ?? attr.DefaultWorkflowStorageProvider;
                var value = await _workflowStorageService.LoadAsync(providerName, workflowStorageContext, propertyName, cancellationToken);

                if (value != null)
                {
                    var typedValue = value.ConvertTo(property.PropertyType);
                    property.SetValue(activity, typedValue);
                }
            }

            foreach (var nestedProperty in nestedProperties)
            {
                var instance = Activator.CreateInstance(nestedProperty.PropertyType);
                var propertyName = parentName == null ? nestedProperty.Name : $"{parentName}_{nestedProperty.Name}";
                await ApplyStoredObjectValuesAsync(context, instance, cancellationToken, propertyName);
            }
        }

        private async ValueTask StoreAppliedValuesAsync(ActivityExecutionContext context, IActivity activity, CancellationToken cancellationToken)
        {
            await StoreAppliedObjectValuesAsync(context, activity, activity, cancellationToken);
        }

        /// <summary>
        /// Recursively store activity's properties
        /// </summary>
        /// <param name="activity">The parent activity of all the activity properties</param>
        /// <param name="nestedInstance">The activity or the recursively generated object from the activity's properties</param>
        private async ValueTask StoreAppliedObjectValuesAsync(ActivityExecutionContext context, IActivity activity, object nestedInstance, CancellationToken cancellationToken, string? parentName = null)
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var properties = nestedInstance.GetType().GetProperties().Where(IsActivityProperty).ToList();
            var nestedProperties = nestedInstance.GetType().GetProperties().Where(IsActivityObjectProperty).ToList();
            var propertyStorageProviderDictionary = context.ActivityBlueprint.PropertyStorageProviders;
            var workflowStorageContext = new WorkflowStorageContext(context.WorkflowInstance, context.ActivityId);

            foreach (var property in properties)
            {
                var propertyName = parentName == null ? property.Name : $"{parentName}_{property.Name}";

                var serializingProperty = new SerializingProperty(context.WorkflowExecutionContext.WorkflowBlueprint, activity.Id, propertyName);
                await mediator.Publish(serializingProperty, cancellationToken);
                if (!serializingProperty.CanSerialize)
                {
                    continue;
                }

                var value = property.GetValue(nestedInstance);
                var attr = property.GetCustomAttributes<ActivityPropertyAttributeBase>().First();
                var providerName = propertyStorageProviderDictionary.GetItem(propertyName) ?? attr.DefaultWorkflowStorageProvider;
                await _workflowStorageService.SaveAsync(providerName, workflowStorageContext, propertyName, value, cancellationToken);
            }

            foreach (var nestedProperty in nestedProperties)
            {
                var instance = Activator.CreateInstance(nestedProperty.PropertyType);
                var propertyName = parentName == null ? nestedProperty.Name : $"{parentName}_{nestedProperty.Name}";
                await StoreAppliedObjectValuesAsync(context, activity, instance, cancellationToken, propertyName);
            }
        }

        private bool ShouldSetProperties(ActivityExecutionContext context, IActivity activity)
        {
            if (IsReturningComposite(activity))
                return false;

            if (IsReturningIf(activity))
            {
                activity.Data!.SetState("Unwinding", false);
                return false;
            }

            if (IsReturningSwitch(activity))
            {
                activity.Data!.SetState("Unwinding", false);
                return false;
            }

            return true;
        }

        private IEnumerable<PropertyInfo> GetNestedProperties(object activity)
        {
            var properties = activity.GetType().GetProperties().Where(IsActivityProperty).ToList();
            var objectProperties = activity.GetType().GetProperties().Where(IsActivityObjectProperty).ToList();

            foreach (var property in properties)
            {
                yield return property;
            }

            foreach (var property in objectProperties)
            {
                var child = GetNestedProperties(property);

                foreach (var ch in child)
                {
                    yield return ch;
                }
            }
        }

        private bool IsReturningComposite(IActivity activity) => activity is CompositeActivity && activity.Data!.GetState<bool>(nameof(CompositeActivity.IsScheduled));
        private bool IsReturningIf(IActivity activity) => activity is If && activity.Data!.GetState<bool>("Unwinding");
        private bool IsReturningSwitch(IActivity activity) => activity is Switch && activity.Data!.GetState<bool>("Unwinding");
        private bool IsActivityProperty(PropertyInfo property) => (property.GetCustomAttribute<ActivityInputAttribute>() != null || property.GetCustomAttribute<ActivityOutputAttribute>() != null) && property.GetCustomAttribute<NonPersistableAttribute>() == null;
        private bool IsActivityObjectProperty(PropertyInfo property) => (property.GetCustomAttribute<ActivityInputObjectAttribute>() != null) && property.GetCustomAttribute<NonPersistableAttribute>() == null;
    }
}