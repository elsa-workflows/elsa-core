using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Elsa.Triggers;

namespace Elsa
{
    public static class WorkflowRunnerExtensions
    {
        public static Task TriggerWorkflowsAsync<T>(
            this IWorkflowRunner workflowRunner,
            ITrigger trigger,
            string? tenantId,
            object? input = default,
            string? correlationId = default,
            string? contextId = default,
            CancellationToken cancellationToken = default) where T : IActivity =>
            workflowRunner.TriggerWorkflowsAsync(typeof(T).Name, trigger, tenantId, input, correlationId, contextId, cancellationToken);
    }
}