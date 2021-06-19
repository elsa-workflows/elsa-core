using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Bookmarks;

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
        public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<EntityChanged> context, CancellationToken cancellationToken) =>
            new[]
            {
                Result(new EntityChangedBookmark(
                    entityName: await context.ReadActivityPropertyAsync(x => x.EntityName, cancellationToken),
                    action: await context.ReadActivityPropertyAsync(x => x.Action, cancellationToken),
                    contextId: context.ActivityExecutionContext.WorkflowExecutionContext.WorkflowInstance.ContextId,
                    correlationId: context.ActivityExecutionContext.WorkflowExecutionContext.WorkflowInstance.CorrelationId
                ))
            };
    }
}