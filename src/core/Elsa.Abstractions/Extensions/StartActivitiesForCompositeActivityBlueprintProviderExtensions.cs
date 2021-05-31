using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa
{
    public static class StartActivitiesForCompositeActivityBlueprintProviderExtensions
    {
        public static IEnumerable<IActivityBlueprint> GetStartActivities(this IGetsStartActivities startActivitiesProvider,
                                                                         ICompositeActivityBlueprint workflowBlueprint,
                                                                         string activityType)
            => startActivitiesProvider.GetStartActivities(workflowBlueprint).Where(x => x.Type == activityType);

        public static IEnumerable<IActivityBlueprint> GetStartActivities(this IGetsStartActivities startActivitiesProvider,
                                                                         ICompositeActivityBlueprint workflowBlueprint,
                                                                         Type activityType)
            => startActivitiesProvider.GetStartActivities(workflowBlueprint, activityType.Name);

        public static IEnumerable<IActivityBlueprint> GetStartActivities<T>(this IGetsStartActivities startActivitiesProvider,
                                                                            ICompositeActivityBlueprint workflowBlueprint) where T : IActivity
            => startActivitiesProvider.GetStartActivities(workflowBlueprint, typeof(T));
    }
}