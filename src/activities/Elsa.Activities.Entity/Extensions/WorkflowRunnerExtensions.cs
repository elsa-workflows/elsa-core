using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Entity.Bookmarks;
using Elsa.Activities.Entity.Models;
using Elsa.Services.Dispatch;

namespace Elsa.Activities.Entity.Extensions
{
    public static class WorkflowRunnerExtensions
    {
        // TODO: Design multi-tenancy 
        private const string? TenantId = default;

        public static async Task TriggerEntityChangedWorkflowsAsync(
            this IWorkflowDispatcher workflowDispatcher,
            string entityId,
            string entityName,
            EntityChangedAction changedAction,
            string? correlationId = default,
            string? contextId = default,
            CancellationToken cancellationToken = default)
        {
            const string activityType = nameof(EntityChanged);
            var input = new EntityChangedContext(entityId, entityName, changedAction);

            var trigger = new EntityChangedBookmark(
                entityName,
                changedAction,
                contextId,
                null
            );

            var bookmark = new EntityChangedBookmark(
                entityName,
                changedAction,
                contextId,
                correlationId
            );

            await workflowDispatcher.DispatchAsync(new TriggerWorkflowsRequest(activityType, bookmark, trigger, input, correlationId, default, contextId, TenantId), cancellationToken);
        }
    }
}