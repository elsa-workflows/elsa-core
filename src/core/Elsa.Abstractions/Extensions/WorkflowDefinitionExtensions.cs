using System.Collections.Generic;
using System.Linq;
using Elsa.Models;

namespace Elsa
{
    public static class WorkflowDefinitionExtensions
    {
        public static ActivityDefinition
            GetActivityById(this WorkflowDefinition workflowDefinition, string activityId) =>
            workflowDefinition.Activities.First(x => x.ActivityId == activityId);

        // TODO: Consider adding this as an overload for IGetsStartActivitiesForCompositeActivityBlueprint
        // The intent is the same and the logic is near-identical.
        public static IEnumerable<ActivityDefinition> GetStartActivities(this WorkflowDefinition workflowDefinition)
        {
            var targetActivities = workflowDefinition.Connections
                .Select(x => x.TargetActivityId)
                .Where(x => x != null)
                .Distinct()
                .ToLookup(x => x);

            var query =
                from activity in workflowDefinition.Activities
                where !targetActivities.Contains(activity.ActivityId)
                select activity;

            return query;
        }
    }
}