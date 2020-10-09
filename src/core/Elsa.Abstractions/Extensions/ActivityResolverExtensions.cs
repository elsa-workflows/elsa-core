using Elsa.Models;
using Elsa.Services;

namespace Elsa
{
    public static class ActivityResolverExtensions
    {
        public static IActivity ResolveActivity(this IActivityResolver activityResolver, ActivityDefinition activityDefinition)
        {
            var activity = activityResolver.ResolveActivity(activityDefinition.Type);
            activity.Description = activityDefinition.Description;
            activity.Id = activityDefinition.Id;
            activity.Name = activityDefinition.Name;
            activity.DisplayName = activityDefinition.DisplayName;
            activity.PersistWorkflow = activityDefinition.PersistWorkflow;
            return activity;
        }
    }
}