using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Entity.Bookmarks;
using Elsa.Activities.Entity.Models;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Entity.Extensions
{
    public static class WorkflowRunnerExtensions
    {
        public static async Task TriggerEntityChangedWorkflowsAsync(
            this IWorkflowLaunchpad workflowLaunchpad,
            string entityId,
            string entityName,
            EntityChangedAction changedAction,
            string? correlationId = default,
            string? contextId = default,
            CancellationToken cancellationToken = default)
        {
            var input = new EntityChangedContext(entityId, entityName, changedAction);
            var query = QueryEntityChangedWorkflowsAsync(entityName, changedAction, correlationId, contextId);
            await workflowLaunchpad.CollectAndExecuteWorkflowsAsync(query, new WorkflowInput(input), cancellationToken);
        }
        
        public static async Task DispatchEntityChangedWorkflowsAsync(
            this IWorkflowLaunchpad workflowLaunchpad,
            string entityId,
            string entityName,
            EntityChangedAction changedAction,
            string? correlationId = default,
            string? contextId = default,
            CancellationToken cancellationToken = default)
        {
            var input = new EntityChangedContext(entityId, entityName, changedAction);
            var query = QueryEntityChangedWorkflowsAsync(entityName, changedAction, correlationId, contextId);
            await workflowLaunchpad.CollectAndExecuteWorkflowsAsync(query, new WorkflowInput(input), cancellationToken);
        }

        private static WorkflowsQuery QueryEntityChangedWorkflowsAsync(
            string entityName,
            EntityChangedAction changedAction,
            string? correlationId = default,
            string? contextId = default)
        {
            const string activityType = nameof(EntityChanged);

            var bookmark = new EntityChangedBookmark(
                entityName,
                changedAction
            );

            return new WorkflowsQuery(activityType, bookmark, correlationId, ContextId: contextId);
        }
    }
}