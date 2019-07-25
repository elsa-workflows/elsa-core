using System.Collections.Generic;
using System.Linq;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Extensions
{
    public static class WorkflowInstanceCollectionExtensions
    {
        public static IEnumerable<(WorkflowInstance, ActivityInstance)> GetBlockingActivities(this IEnumerable<WorkflowInstance> instances)
        {
            var query =
                from workflowInstance in instances
                from blockingActivity in workflowInstance.BlockingActivities
                let activity = workflowInstance.Activities[blockingActivity.ActivityId]
                select (workflowInstance, activity);

            return query.Distinct();
        }
    }
}