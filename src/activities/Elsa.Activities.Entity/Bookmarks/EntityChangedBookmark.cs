using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Bookmarks;

namespace Elsa.Activities.Entity.Bookmarks
{
    public class EntityChangedBookmark : IBookmark
    {
        public EntityChangedBookmark(string? entityName, EntityChangedAction? action, string? contextId, string? correlationId)
        {
            EntityName = entityName;
            Action = action;
            ContextId = contextId;
            CorrelationId = correlationId;
        }

        public string? EntityName { get; }
        public EntityChangedAction? Action { get; }
        public string? ContextId { get; }
        public string? CorrelationId { get; }
    }

    public class EntityChangedWorkflowTriggerProvider : BookmarkProvider<EntityChangedBookmark, EntityChanged>
    {
        public override async ValueTask<IEnumerable<IBookmark>> GetBookmarksAsync(BookmarkProviderContext<EntityChanged> context, CancellationToken cancellationToken) =>
            new[]
            {
                new EntityChangedBookmark(
                    entityName: await context.Activity.GetPropertyValueAsync(x => x.EntityName, cancellationToken),
                    action: await context.Activity.GetPropertyValueAsync(x => x.Action, cancellationToken),
                    contextId: context.ActivityExecutionContext.WorkflowExecutionContext.WorkflowInstance.ContextId,
                    correlationId: context.ActivityExecutionContext.WorkflowExecutionContext.WorkflowInstance.CorrelationId
                )
            };
    }
}