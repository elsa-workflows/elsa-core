using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Entity.Bookmarks;
using Elsa.Activities.Entity.Models;
using Elsa.Services;

namespace Elsa.Activities.Entity.Extensions
{
    public static class WorkflowRunnerExtensions
    {
        // TODO: Figure out how to start jobs across multiple tenants / how to get a list of all tenants. 
        private const string TenantId = default;
        
        public static async Task TriggerEntityChangedWorkflowsAsync(
            this IWorkflowRunner workflowRunner,
            string entityId,
            string entityName,
            EntityChangedAction changedAction,
            string? correlationId = default,
            string? contextId = default,
            CancellationToken cancellationToken = default)
        {
            var input = new EntityChangedContext(entityId, entityName, changedAction);

            var trigger = new EntityChangedBookmark(
            
                entityName,
                changedAction,
                contextId,
                correlationId
            );

            await workflowRunner.TriggerWorkflowsAsync<EntityChanged>(
                trigger,
                TenantId,
                input,
                correlationId,
                contextId,
                cancellationToken);
        }
    }
}