using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Services;

namespace Elsa.Activities.Entity.Bookmarks
{
    public class EntityChangedBookmark : IBookmark
    {
        public EntityChangedBookmark(string? entityName, EntityChangedAction? action)
        {
            EntityName = entityName;
            Action = action;
        }

        [ExcludeFromHash] public string? EntityName { get; }
        public EntityChangedAction? Action { get; }

        public bool? Compare(IBookmark bookmark)
        {
            return bookmark is EntityChangedBookmark other
                   && string.Equals(EntityName, other.EntityName, System.StringComparison.OrdinalIgnoreCase)
                   && Action == other.Action;
        }
    }

    public class EntityChangedWorkflowTriggerProvider : BookmarkProvider<EntityChangedBookmark, EntityChanged>
    {
        public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<EntityChanged> context, CancellationToken cancellationToken)
        {
            var entityName = await context.ReadActivityPropertyAsync(x => x.EntityName, cancellationToken);
            var action = await context.ReadActivityPropertyAsync(x => x.Action, cancellationToken);

            var bookmark = new EntityChangedBookmark(
                entityName,
                action
            );

            return new[] { Result(bookmark) };
        }
    }
}