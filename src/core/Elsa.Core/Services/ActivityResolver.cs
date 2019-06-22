using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Core.Activities;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Core.Services
{
    public class ActivityResolver : IActivityResolver
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IIdGenerator idGenerator;
        private readonly IDictionary<string, Type> activityTypeLookup;

        public ActivityResolver(IServiceProvider serviceProvider, IEnumerable<IActivity> activities, IIdGenerator idGenerator)
        {
            this.serviceProvider = serviceProvider;
            this.idGenerator = idGenerator;
            activityTypeLookup = activities.Select(x => x.GetType()).ToDictionary(x => x.Name);
        }
        
        public IActivity ResolveActivity(string activityTypeName)
        {
            if (!activityTypeLookup.ContainsKey(activityTypeName))
                return serviceProvider.GetRequiredService<UnknownActivity>();

            var type = activityTypeLookup[activityTypeName];
            var activity = (IActivity)serviceProvider.GetRequiredService(type);

            activity.Id = idGenerator.Generate();
            return activity;
        }
    }
}