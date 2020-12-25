﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
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

        public IDictionary<string, IActivityPropertyValueProvider>? GetProviders(string activityId) => _providers.TryGetValue(activityId, out var properties) ? properties : null;

        public IActivityPropertyValueProvider? GetProvider(string activityId, string propertyName) =>
            _providers.TryGetValue(activityId, out var properties) 
            && properties != null 
            && properties.TryGetValue(propertyName, out var provider) 
                ? provider 
                : null;

        public async ValueTask SetActivityPropertiesAsync(IActivity activity, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken = default)
        {
            var properties = activity.GetType().GetProperties().Where(IsActivityProperty).ToList();
            var providers = GetProviders(activity.Id);

            if (providers == null)
                return;

            foreach (var property in properties)
            {
                if (!providers.TryGetValue(property.Name, out var provider))
                    continue;

                var value = await provider.GetValueAsync(activityExecutionContext, cancellationToken);
                property.SetValue(activity, value);
            }
        }

        private bool IsActivityProperty(PropertyInfo property) => property.GetCustomAttribute<ActivityPropertyAttribute>() != null;
        public IEnumerator<KeyValuePair<string, IDictionary<string, IActivityPropertyValueProvider>>> GetEnumerator() => _providers.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}