// using System;
// using System.Collections.Generic;
// using System.Threading;
// using System.Threading.Tasks;
// using Elsa.Models;
// using Elsa.Triggers;
//
// namespace Elsa.Services
// {
//     public interface IWorkflowScheduler
//     {
//         /// <summary>
//         /// Schedules new and existing instances matching the specified trigger.
//         /// </summary>
//         Task TriggerWorkflowsAsync<TTrigger>(Func<TTrigger, bool> predicate, object? input = default, string? correlationId = default, string? contextId = default, CancellationToken cancellationToken = default) where TTrigger : ITrigger;
//
//         /// <summary>
//         /// Schedules the specified workflow instance for execution.
//         /// </summary>
//         Task ScheduleWorkflowInstanceAsync(string instanceId, string? activityId = default, object? input = default, CancellationToken cancellationToken = default);
//
//         /// <summary>
//         /// Creates new workflow instances of the specified workflow definition for each start activity and schedules it for execution.
//         /// </summary>
//         Task<IEnumerable<WorkflowInstance>> ScheduleWorkflowDefinitionAsync(
//             string definitionId, 
//             string? tenantId, 
//             object? input = default, 
//             string? correlationId = default, 
//             string? contextId = default,
//             CancellationToken cancellationToken = default);
//
//         /// <summary>
//         /// Creates a new workflow instances of the specified workflow definition for the specified start activity and schedules it for execution.
//         /// </summary>
//         Task<IEnumerable<WorkflowInstance>> ScheduleWorkflowDefinitionAsync(
//             string definitionId,
//             string activityId,
//             string? tenantId,
//             object? input = default,
//             string? correlationId = default,
//             string? contextId = default,
//             CancellationToken cancellationToken = default);
//
//         /// <summary>
//         /// Schedules new workflows that start with the specified activity type or are blocked on the specified activity type.
//         /// </summary>
//         Task TriggerWorkflowsAsync(string activityType, object? input = default, string? correlationId = default, string? contextId = default, CancellationToken cancellationToken = default);
//     }
// }