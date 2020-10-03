using System;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Services
{
    public interface IWorkflowScheduler
    {
        /// <summary>
        /// Schedules the specified workflow instance for execution.
        /// </summary>
        Task ScheduleWorkflowAsync(string instanceId, string? activityId = default, object? input = default, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Creates a new workflow instance of the specified workflow definition and schedules it for execution.
        /// </summary>
        Task ScheduleNewWorkflowAsync(string definitionId, object? input = default, string? correlationId = default, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Schedules new workflows that start with the specified activity type or are blocked on the specified activity type.
        /// </summary>
        Task TriggerWorkflowsAsync(string activityType, object? input = default, string? correlationId = default, Func<IActivity, bool>? activityStatePredicate = default, CancellationToken cancellationToken = default);
    }
}