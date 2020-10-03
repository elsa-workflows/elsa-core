using Elsa.Models;
using Elsa.Services;

namespace Elsa
{
    public static class ActivityResolverExtensions
    {
        public static IActivity ResolveActivity(this IActivityResolver activityResolver, ActivityDefinitionRecord activityDefinitionRecord)
        {
            var activity = activityResolver.ResolveActivity(activityDefinitionRecord.Type);
            activity.Description = activityDefinitionRecord.Description;
            activity.Id = activityDefinitionRecord.Id;
            activity.Name = activityDefinitionRecord.Name;
            activity.DisplayName = activityDefinitionRecord.DisplayName;
            activity.PersistWorkflow = activityDefinitionRecord.PersistWorkflow;
            return activity;
        }
    }
}