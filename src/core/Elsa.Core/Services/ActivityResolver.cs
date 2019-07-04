using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Core.Services
{
    public class ActivityResolver : IActivityResolver
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IDictionary<string, Type> activityTypeLookup;

        public ActivityResolver(IServiceProvider serviceProvider, IEnumerable<IActivity> activities)
        {
            this.serviceProvider = serviceProvider;
            activityTypeLookup = activities.Select(x => x.GetType()).ToDictionary(x => x.Name);
        }
        
        public Type ResolveActivityType(string activityTypeName)
        {
            if (!activityTypeLookup.ContainsKey(activityTypeName))
            {
                var activityType = Type.GetType(activityTypeName);
                
                if(activityType == null)
                    throw new ArgumentException($"No such activity type: {activityTypeName}", nameof(activityTypeName));
                
                activityTypeLookup[activityTypeName] = activityType;
            }

            return activityTypeLookup[activityTypeName];    

        }
        
        public IActivity ResolveActivity(string activityTypeName, Action<IActivity> setup = null)
        {
            var activityType = ResolveActivityType(activityTypeName);
            var activity = (IActivity) ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, activityType);

            setup?.Invoke(activity);
            return activity;
        }
        
        public T ResolveActivity<T>(Action<T> setup = null) where T : class, IActivity
        {
            return (T) ResolveActivity(typeof(T).Name, setup != null ? x => setup((T) x) : default(Action<IActivity>));
        }
    }
}