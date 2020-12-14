using System.Threading;
using System.Threading.Tasks;

using Elsa.Triggers;

namespace Elsa.Activities.Entity.Triggers
{
    public class EntityChangedTrigger : Trigger
    {   
        public string? EntityName { get; set; }

        public EntityChangedAction? Action { get; set; }

        public string? ContextId { get; set; }

        public string? CorrelationId { get; set; }

        public EntityChangedTrigger()
        {
        }

        public EntityChangedTrigger(string? entityName, EntityChangedAction? action, string? contextId, string? correlationId)
        {
            EntityName = entityName;
            Action = action;
            ContextId = contextId;
            CorrelationId = correlationId;
        }
    }

    public class EntityChangedTriggerProvider : TriggerProvider<EntityChangedTrigger, EntityChanged>
    {
        public override async ValueTask<ITrigger> GetTriggerAsync(TriggerProviderContext<EntityChanged> context, CancellationToken cancellationToken) =>
            new EntityChangedTrigger(
               entityName: await context.Activity.GetPropertyValueAsync(x => x.EntityName, cancellationToken),
               action: await context.Activity.GetPropertyValueAsync(x => x.Action, cancellationToken),
               contextId: context.ActivityExecutionContext.WorkflowExecutionContext.WorkflowInstance.ContextId,
               correlationId: context.ActivityExecutionContext.WorkflowExecutionContext.WorkflowInstance.CorrelationId
           );
    }
}
