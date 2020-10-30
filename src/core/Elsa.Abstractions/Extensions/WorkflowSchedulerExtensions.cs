using System;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Elsa.Services
{
    public static class WorkflowSchedulerExtensions
    {
        /// <summary>
        /// Schedules a new workflow for execution using the workflow definition type.
        /// </summary>
        public static Task RunAsync<T>(this IWorkflowScheduler workflowScheduler,
            object? input = default,
            string? correlationId = default,
            CancellationToken cancellationToken = default)
            => workflowScheduler.ScheduleWorkflowDefinitionAsync(typeof(T).Name, input, correlationId, cancellationToken);

        /// <summary>
        /// Schedules new workflows that start with the specified activity type or are blocked on the specified activity type.
        /// </summary>
        public static Task TriggerWorkflowsAsync<T>(this IWorkflowScheduler workflowScheduler,
            object? input = default,
            string? correlationId = default,
            Func<T, bool>? activityStatePredicate = default,
            CancellationToken cancellationToken = default) where T : IActivity
        {
            return workflowScheduler.TriggerWorkflowsAsync(
                typeof(T).Name,
                input,
                correlationId,
                cancellationToken);
        }
    }
}