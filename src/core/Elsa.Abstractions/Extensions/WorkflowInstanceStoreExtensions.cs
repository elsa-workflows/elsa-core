using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services;

namespace Elsa.Extensions
{
    public static class WorkflowInstanceStoreExtensions
    {
        public static Task<IEnumerable<(WorkflowInstance WorkflowInstance, IActivity BlockingActivity)>> ListByBlockingActivityAsync<TActivity>(
            this IWorkflowInstanceStore store,
            string? correlationId = default,
            Func<IActivity, bool>? activityStatePredicate = default,
            CancellationToken cancellationToken = default) where TActivity : IActivity =>
            store.ListByBlockingActivityAsync(typeof(TActivity).Name, correlationId, activityStatePredicate, cancellationToken);

        public static async Task<IEnumerable<(WorkflowInstance WorkflowInstance, IActivity BLockingActivity)>> ListByBlockingActivityAsync(
            this IWorkflowInstanceStore store,
            string activityType,
            string? correlationId,
            Func<IActivity, bool>? activityStatePredicate = default,
            CancellationToken cancellationToken = default)
        {
            var tuples = await store.ListByBlockingActivityAsync(activityType, correlationId, cancellationToken);
            var query =
                from item in tuples
                let workflowInstance = item.WorkflowInstance
                let blockingActivity = item.BlockingActivity
                let activityInstance = workflowInstance.Activities.First(x => x.Id == blockingActivity.ActivityId)
                let activity = (IActivity)null
                select (workflowInstance, activity);

            if (activityStatePredicate != null)
            {
                query = query.Where(tuple => activityStatePredicate(tuple.activity));
            }

            return query;
        }
    }
}