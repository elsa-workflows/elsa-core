using System.Collections.Generic;
using System.Linq;
using Elsa.Models;

namespace Elsa.Extensions
{
    public static class WorkflowInstanceCollectionExtensions
    {
        public static IEnumerable<(WorkflowInstance, ActivityInstance)> GetBlockingActivities(this IEnumerable<WorkflowInstance> instances, string activityType = null)
        {
            var query =
                from workflowInstance in instances
                from blockingActivity in workflowInstance.BlockingActivities
                let activityDictionary = workflowInstance.Activities.ToDictionary(x => x.Id)
                let activity = activityDictionary[blockingActivity.ActivityId]
                select (workflowInstance, activity);

            if (!string.IsNullOrWhiteSpace(activityType))
                query = query.Where(x => x.activity.Type == activityType);

            return query.Distinct();
        }
    }
}