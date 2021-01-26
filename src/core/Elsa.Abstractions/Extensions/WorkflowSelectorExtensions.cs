using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Elsa.Triggers;

namespace Elsa
{
    public static class WorkflowSelectorExtensions
    {
        public static Task<IEnumerable<WorkflowSelectorResult>> SelectWorkflowsAsync<T>(
            this IWorkflowSelector workflowSelector,
            IEnumerable<ITrigger> triggers,
            string? tenantId,
            CancellationToken cancellationToken = default) where T : IActivity =>
            workflowSelector.SelectWorkflowsAsync(typeof(T).Name, triggers, tenantId, cancellationToken);

        public static Task<IEnumerable<WorkflowSelectorResult>> SelectWorkflowsAsync<T>(
            this IWorkflowSelector workflowSelector,
            ITrigger trigger,
            string? tenantId,
            CancellationToken cancellationToken = default) where T : IActivity =>
            workflowSelector.SelectWorkflowsAsync(typeof(T).Name, new[] { trigger }, tenantId, cancellationToken);
        
        public static Task<IEnumerable<WorkflowSelectorResult>> SelectWorkflowsAsync(
            this IWorkflowSelector workflowSelector,
            string activityType,
            ITrigger trigger,
            string? tenantId,
            CancellationToken cancellationToken = default) =>
            workflowSelector.SelectWorkflowsAsync(activityType, new[] { trigger }, tenantId, cancellationToken);

        public static Task<IEnumerable<WorkflowSelectorResult>> SelectWorkflowsAsync<T>(
            this IWorkflowSelector workflowSelector,
            string? tenantId,
            CancellationToken cancellationToken = default) where T : IActivity =>
            workflowSelector.SelectWorkflowsAsync(typeof(T).Name, Enumerable.Empty<ITrigger>(), tenantId, cancellationToken);
    }
}