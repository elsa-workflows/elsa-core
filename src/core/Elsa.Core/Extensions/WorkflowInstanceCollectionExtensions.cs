using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Models;

namespace Elsa.Extensions
{
    public static class WorkflowInstanceCollectionExtensions
    {
        public static IEnumerable<(ProcessInstance, ActivityInstance)> GetBlockingActivities(this IEnumerable<ProcessInstance> instances, string activityType = null)
        {
            // var query =
            //     from processInstance in instances
            //     from blockingActivity in processInstance.BlockingActivities
            //     let activityDictionary = processInstance.Activities.ToDictionary(x => x.Id)
            //     let activity = activityDictionary[blockingActivity.ActivityId]
            //     select (workflowInstance, activity);
            //
            // if (!string.IsNullOrWhiteSpace(activityType))
            //     query = query.Where(x => x.activity.Type == activityType);
            //
            // return query.Distinct();
            
            throw new NotImplementedException();
        }
    }
}