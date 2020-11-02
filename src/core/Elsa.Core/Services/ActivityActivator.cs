using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Services
{
    public class ActivityActivator : IActivityActivator
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Lazy<IDictionary<string, Type>> _lazyActivityTypeLookup;

        public ActivityActivator(IServiceProvider serviceProvider, Func<IEnumerable<IActivity>> activitiesFunc)
        {
            _serviceProvider = serviceProvider;
            _lazyActivityTypeLookup = new Lazy<IDictionary<string, Type>>(
                () =>
                {
                    var activities = activitiesFunc();
                    return activities.Select(x => x.GetType()).Distinct().ToDictionary(x => x.Name);
                });
        }

        private IDictionary<string, Type> ActivityTypeLookup => _lazyActivityTypeLookup.Value;

        public Type GetActivityTypeByName(string activityTypeName)
        {
            if (!ActivityTypeLookup.ContainsKey(activityTypeName))
            {
                var activityType = Type.GetType(activityTypeName);

                if (activityType == null)
                    throw new ArgumentException($"No such activity type: {activityTypeName}", nameof(activityTypeName));

                ActivityTypeLookup[activityTypeName] = activityType;
            }

            return ActivityTypeLookup[activityTypeName];
        }

        public IActivity ActivateActivity(string activityTypeName, Action<IActivity>? setup = null)
        {
            var activityType = GetActivityTypeByName(activityTypeName);
            var activity = (IActivity)ActivatorUtilities.GetServiceOrCreateInstance(_serviceProvider, activityType);

            setup?.Invoke(activity);
            return activity;
        }

        public T ActivateActivity<T>(Action<T>? setup = null) where T : class, IActivity
        {
            var activity = ActivatorUtilities.GetServiceOrCreateInstance<T>(_serviceProvider);
            setup?.Invoke(activity);
            return activity;
        }

        public IActivity ActivateActivity(ActivityDefinition activityDefinition)
        {
            var activity = ActivateActivity(activityDefinition.Type);
            activity.Description = activityDefinition.Description;
            activity.Id = activityDefinition.ActivityId;
            activity.Name = activityDefinition.Name;
            activity.DisplayName = activityDefinition.DisplayName;
            activity.PersistWorkflow = activityDefinition.PersistWorkflow;
            return activity;
        }

        public IEnumerable<Type> GetActivityTypes() => ActivityTypeLookup.Values.ToList();
        public Type? GetActivityType(string activityTypeName) => ActivityTypeLookup.ContainsKey(activityTypeName) ? ActivityTypeLookup[activityTypeName] : default;
    }
}