using System.Collections.Generic;
using System.Linq;
using Elsa.Models;

namespace Elsa
{
    public static class WorkflowInstanceCollectionExtensions
    {
        public static IEnumerable<(WorkflowInstance, BlockingActivity)> GetBlockingActivities(
            this IEnumerable<WorkflowInstance> instances,
            string? activityType = null)
        {
            var query =
                from workflowInstance in instances
                from blockingActivity in workflowInstance.BlockingActivities
                select (workflowInstance, blockingActivity);

            if (!string.IsNullOrWhiteSpace(activityType))
                query = query.Where(x => x.blockingActivity.ActivityType == activityType);

            return query.Distinct();
        }
    }
}