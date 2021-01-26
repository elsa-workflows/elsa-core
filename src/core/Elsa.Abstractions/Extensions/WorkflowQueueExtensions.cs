using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Elsa.Triggers;

namespace Elsa
{
    public static class WorkflowQueueExtensions
    {
        public static Task EnqueueWorkflowsAsync<T>(
            this IWorkflowQueue workflowQueue,
            ITrigger trigger,
            string? tenantId,
            object? input = default,
            string? correlationId = default,
            string? contextId = default,
            CancellationToken cancellationToken = default) where T : IActivity =>
            workflowQueue.EnqueueWorkflowsAsync(typeof(T).Name, trigger, tenantId, input, correlationId, contextId, cancellationToken);
    }
}