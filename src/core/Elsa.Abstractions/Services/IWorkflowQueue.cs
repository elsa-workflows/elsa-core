using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Bookmarks;

namespace Elsa.Services
{
    public interface IWorkflowQueue
    {
        /// <summary>
        /// Selects workflows and workflow instances based on the specified trigger predicate and enqueues the results for execution.
        /// </summary>
        Task EnqueueWorkflowsAsync(
            string activityType,
            IBookmark bookmark,
            string? tenantId,
            object? input = default,
            string? correlationId = default,
            string? contextId = default,
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Enqueues the specified workflows for execution.
        /// </summary>
        Task EnqueueWorkflowsAsync(
            IEnumerable<BookmarkFinderResult> results,
            object? input = default,
            string? correlationId = default,
            string? contextId = default,
            CancellationToken cancellationToken = default);
        
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