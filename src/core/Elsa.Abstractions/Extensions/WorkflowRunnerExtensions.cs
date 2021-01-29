using System.Threading;
using System.Threading.Tasks;
using Elsa.Bookmarks;
using Elsa.Services;

namespace Elsa
{
    public static class WorkflowRunnerExtensions
    {
        public static Task TriggerWorkflowsAsync<T>(
            this IWorkflowRunner workflowRunner,
            IBookmark bookmark,
            string? tenantId,
            object? input = default,
            string? correlationId = default,
            string? contextId = default,
            CancellationToken cancellationToken = default) where T : IActivity =>
            workflowRunner.TriggerWorkflowsAsync(typeof(T).Name, bookmark, tenantId, input, correlationId, contextId, cancellationToken);
    }
}