using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Triggers;

namespace Elsa.Services
{
    public interface IWorkflowScheduler
    {
        /// <summary>
        /// Schedules new and existing instances matching the specified trigger.
        /// </summary>
        Task TriggerWorkflowsAsync<TTrigger>(Func<TTrigger, bool> predicate, object? input = default, string? correlationId = default, string? contextId = default, CancellationToken cancellationToken = default) where TTrigger : ITrigger;
        
        /// <summary>
        /// Schedules the specified workflow instance for execution.
        /// </summary>
        Task ScheduleWorkflowInstanceAsync(string instanceId, string? activityId = default, object? input = default, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Creates a new workflow instance of the specified workflow definition and schedules it for execution.
        /// </summary>
        Task ScheduleWorkflowDefinitionAsync(string definitionId, object? input = default, string? correlationId = default, string? contextId = default, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Schedules new workflows that start with the specified activity type or are blocked on the specified activity type.
        /// </summary>
        Task TriggerWorkflowsAsync(string activityType, object? input = default, string? correlationId = default, string? contextId = default, CancellationToken cancellationToken = default);
    }
}