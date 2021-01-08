using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Triggers;

namespace Elsa.Services
{
    public interface IWorkflowQueue
    {
        /// <summary>
        /// Selects workflows and workflow instances based on the specified trigger predicate and enqueues the results for execution.
        /// </summary>
        Task EnqueueWorkflowsAsync<TTrigger>(
            Func<TTrigger, bool> predicate,
            object? input = default,
            string? correlationId = default,
            string? contextId = default,
            CancellationToken cancellationToken = default)
            where TTrigger : ITrigger;
        
        /// <summary>
        /// Enqueues the specified workflow instance and activity for execution.
        /// </summary>
        Task EnqueueWorkflowInstance(string workflowInstanceId, string activityId, object? input, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Enqueues the specified workflow definition and activity for execution.
        /// </summary>
        Task EnqueueWorkflowDefinition(string workflowDefinitionId, string? tenantId, string activityId, object? input, string? correlationId, string? contextId, CancellationToken cancellationToken = default);
    }
}