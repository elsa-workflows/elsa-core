using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Elsa.Services.Bookmarks;

namespace Elsa.Activities.Entity.Bookmarks
{
    public class EntityChangedBookmark : IBookmark
    {
        public EntityChangedBookmark(string? entityName, EntityChangedAction? action, string? contextId)
        {
            EntityName = entityName;
            Action = action;
            ContextId = contextId;
        }

        public string? EntityName { get; }
        public EntityChangedAction? Action { get; }
        public string? ContextId { get; }
    }

    public class EntityChangedWorkflowTriggerProvider : BookmarkProvider<EntityChangedBookmark, EntityChanged>
    {
        public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<EntityChanged> context, CancellationToken cancellationToken) =>
            new[]
            {
                Result(new EntityChangedBookmark(
                    await context.ReadActivityPropertyAsync(x => x.EntityName, cancellationToken),
                    await context.ReadActivityPropertyAsync(x => x.Action, cancellationToken),
                    context.ActivityExecutionContext.WorkflowExecutionContext.WorkflowInstance.ContextId
                ))
            };
    }
}