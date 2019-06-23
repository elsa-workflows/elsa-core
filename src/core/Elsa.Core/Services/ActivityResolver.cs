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
        
        public IActivity ResolveActivity(string activityTypeName, Action<IActivity> setup = null)
        {
            if (!activityTypeLookup.ContainsKey(activityTypeName))
            {
                return null;
            }

            var type = activityTypeLookup[activityTypeName];
            var activity = (IActivity)serviceProvider.GetRequiredService(type);

            setup?.Invoke(activity);
            return activity;
        }
        
        public T ResolveActivity<T>(Action<T> configure = null) where T : class, IActivity
        {
            var activity = (T) ResolveActivity(typeof(T).Name) ?? ActivatorUtilities.CreateInstance<T>(serviceProvider);
            configure?.Invoke(activity);
            return activity;
        }
    }
}