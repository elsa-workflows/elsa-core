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
        public static Task<IEnumerable<(WorkflowInstance WorkflowInstance, BlockingActivity BlockingActivity)>> ListByBlockingActivityAsync<TActivity>(
            this IWorkflowInstanceStore store,
            string? correlationId = default,
            Func<Variables, bool>? activityStatePredicate = default,
            CancellationToken cancellationToken = default) where TActivity : IActivity =>
            store.ListByBlockingActivityAsync(typeof(TActivity).Name, correlationId, activityStatePredicate, cancellationToken);

        public static async Task<IEnumerable<(WorkflowInstance WorkflowInstance, BlockingActivity BLockingActivity)>> ListByBlockingActivityAsync(
            this IWorkflowInstanceStore store,
            string activityType,
            string? correlationId,
            Func<Variables, bool>? activityStatePredicate = default,
            CancellationToken cancellationToken = default)
        {
            var tuples = await store.ListByBlockingActivityAsync(activityType, correlationId, cancellationToken);

            if (activityStatePredicate != null)
            {
                tuples = tuples.Where(tuple =>
                {
                    var activityInstance = tuple.WorkflowInstance.Activities.First(x => x.Id == tuple.BlockingActivity.ActivityId);
                    return activityStatePredicate(activityInstance.State);
                });
            }

            return tuples;
        }
    }
}