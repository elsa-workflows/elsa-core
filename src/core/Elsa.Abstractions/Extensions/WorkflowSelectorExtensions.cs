using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Bookmarks;
using Elsa.Services;

namespace Elsa
{
    public static class WorkflowSelectorExtensions
    {
        public static Task<IEnumerable<WorkflowSelectorResult>> SelectWorkflowsAsync<T>(
            this IWorkflowSelector workflowSelector,
            IEnumerable<IBookmark> triggers,
            string? tenantId,
            CancellationToken cancellationToken = default) where T : IActivity =>
            workflowSelector.SelectWorkflowsAsync(typeof(T).Name, triggers, tenantId, cancellationToken);

        public static Task<IEnumerable<WorkflowSelectorResult>> SelectWorkflowsAsync<T>(
            this IWorkflowSelector workflowSelector,
            IBookmark bookmark,
            string? tenantId,
            CancellationToken cancellationToken = default) where T : IActivity =>
            workflowSelector.SelectWorkflowsAsync(typeof(T).Name, new[] { bookmark }, tenantId, cancellationToken);
        
        public static Task<IEnumerable<WorkflowSelectorResult>> SelectWorkflowsAsync(
            this IWorkflowSelector workflowSelector,
            string activityType,
            IBookmark bookmark,
            string? tenantId,
            CancellationToken cancellationToken = default) =>
            workflowSelector.SelectWorkflowsAsync(activityType, new[] { bookmark }, tenantId, cancellationToken);

        public static Task<IEnumerable<WorkflowSelectorResult>> SelectWorkflowsAsync<T>(
            this IWorkflowSelector workflowSelector,
            string? tenantId,
            CancellationToken cancellationToken = default) where T : IActivity =>
            workflowSelector.SelectWorkflowsAsync(typeof(T).Name, Enumerable.Empty<IBookmark>(), tenantId, cancellationToken);
    }
}