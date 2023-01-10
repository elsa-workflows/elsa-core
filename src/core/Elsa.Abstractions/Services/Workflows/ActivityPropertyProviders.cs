using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Exceptions;
using Elsa.Metadata;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public class ActivityPropertyProviders : IActivityPropertyProviders
    {
        private readonly IDictionary<string, IDictionary<string, IActivityPropertyValueProvider>> _providers =
            new Dictionary<string, IDictionary<string, IActivityPropertyValueProvider>>();

        public ActivityPropertyProviders()
        {
        }

        public ActivityPropertyProviders(IDictionary<string, IDictionary<string, IActivityPropertyValueProvider>> providers)
        {
            _providers = providers;
        }

        public void AddProvider(string activityId, string propertyName, IActivityPropertyValueProvider provider)
        {
            if (!_providers.TryGetValue(activityId, out var properties))
            {
                properties = new Dictionary<string, IActivityPropertyValueProvider>();
                _providers.Add(activityId, properties);
            }

            properties[propertyName] = provider;
        }

        public IDictionary<string, IActivityPropertyValueProvider> GetProviders(string activityId) =>
            _providers.TryGetValue(activityId, out var properties)
                ? properties ?? new Dictionary<string, IActivityPropertyValueProvider>()
                : new Dictionary<string, IActivityPropertyValueProvider>();

        public IActivityPropertyValueProvider? GetProvider(string activityId, string propertyName) =>
            _providers.TryGetValue(activityId, out var properties)
            && properties != null
            && properties.TryGetValue(propertyName, out var provider)
                ? provider
                : null;

        public async ValueTask SetActivityPropertiesAsync(IActivity activity, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken = default)
        {
            var providers = GetProviders(activity.Id);
            var defaultValueResolver = activityExecutionContext.GetService<IActivityPropertyDefaultValueResolver>();

            await SetNestedActivityPropertiesAsync(activity, activityExecutionContext, providers, defaultValueResolver, null, cancellationToken);
        }

        private async ValueTask SetNestedActivityPropertiesAsync(object nestedInstance, ActivityExecutionContext activityExecutionContext, IDictionary<string, IActivityPropertyValueProvider> providers, IActivityPropertyDefaultValueResolver defaultValueResolver, string nestedInstanceName = null, CancellationToken cancellationToken = default)
        {
            var properties = nestedInstance.GetType().GetProperties().Where(IsActivityInputProperty).ToList();
            var nestedProperties = nestedInstance.GetType().GetProperties().Where(IsActivityObjectInputProperty).ToList();

            foreach (var property in properties)
            {
                var propertyName = nestedInstanceName == null ? property.Name : $"{nestedInstanceName}_{property.Name}";
                if (!providers.TryGetValue(propertyName, out var provider))
                    continue;

                try
                {
                    var value = await provider.GetValueAsync(activityExecutionContext, cancellationToken);

                    if (value == null)
                    {
                        value = defaultValueResolver.GetDefaultValue(property);
                    }

                    if (value != null) property.SetValue(nestedInstance, value);
                }
                catch (Exception e)
                {
                    throw new CannotSetActivityPropertyValueException($@"An exception was thrown whilst setting '{nestedInstance.GetType().Name}.{property.Name}'. See the inner exception for further details.", e);
                }
            }

            foreach (var nestedProperty in nestedProperties)
            {
                var instance = Activator.CreateInstance(nestedProperty.PropertyType);

                var nextInstanceName = nestedInstanceName == null ? nestedProperty.Name : $"{nestedInstanceName}_{nestedProperty.Name}";

                await SetNestedActivityPropertiesAsync(instance, activityExecutionContext, providers, defaultValueResolver, nextInstanceName, cancellationToken);
                nestedProperty.SetValue(nestedInstance, instance);
            }
        }
        private bool IsActivityInputProperty(PropertyInfo property) => property.GetCustomAttribute<ActivityInputAttribute>() != null;
        private bool IsActivityObjectInputProperty(PropertyInfo property) => property.GetCustomAttribute<ActivityInputObjectAttribute>() != null;
        public IEnumerator<KeyValuePair<string, IDictionary<string, IActivityPropertyValueProvider>>> GetEnumerator() => _providers.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}