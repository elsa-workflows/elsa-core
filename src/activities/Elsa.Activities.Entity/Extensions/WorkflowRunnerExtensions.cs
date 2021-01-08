using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Entity.Models;
using Elsa.Activities.Entity.Triggers;
using Elsa.Services;

namespace Elsa.Activities.Entity.Extensions
{
    public static class WorkflowRunnerExtensions
    {
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
            bool Filter(EntityChangedTrigger x) => 
                (x.Action == null || x.Action == changedAction) && 
                (x.EntityName == null || x.EntityName == entityName) &&
                (x.ContextId == null || x.ContextId == contextId) &&
                (x.CorrelationId == null || x.CorrelationId == correlationId);

            await workflowRunner.TriggerWorkflowsAsync<EntityChangedTrigger>(
                Filter,
                input,
                correlationId,
                contextId,
                cancellationToken);
        }
    }
}