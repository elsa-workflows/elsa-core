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
        public static Task<IEnumerable<(WorkflowInstance WorkflowInstance, ActivityInstance BlockingActivity)>> ListByBlockingActivityAsync<TActivity>(
            this IWorkflowInstanceStore store,
            string? correlationId = default,
            Func<Variables, bool>? activityStatePredicate = default,
            CancellationToken cancellationToken = default) where TActivity : IActivity =>
            store.ListByBlockingActivityAsync(typeof(TActivity).Name, correlationId, activityStatePredicate, cancellationToken);

        public static async Task<IEnumerable<(WorkflowInstance WorkflowInstance, ActivityInstance BLockingActivity)>> ListByBlockingActivityAsync(
            this IWorkflowInstanceStore store,
            string activityType,
            string? correlationId,
            Func<Variables, bool>? activityStatePredicate = default,
            CancellationToken cancellationToken = default)
        {
            var tuples = await store.ListByBlockingActivityAsync(activityType, correlationId, cancellationToken);
            var query =
                from item in tuples
                let workflowInstance = item.WorkflowInstance
                let blockingActivity = item.BlockingActivity
                let activityInstance = workflowInstance.Activities.First(x => x.Id == blockingActivity.ActivityId)
                select (workflowInstance, activityInstance);

            if (activityStatePredicate != null)
            {
                query = query.Where(tuple => activityStatePredicate(tuple.activityInstance.State));
            }

            return query;
        }
    }
}