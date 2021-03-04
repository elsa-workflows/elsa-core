using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Bookmarks;
using NodaTime;

namespace Elsa.Activities.Temporal.Common.Bookmarks
{
    public class StartAtBookmark : IBookmark
    {
        public Instant ExecuteAt { get; set; }
    }

    public class StartAtBookmarkProvider : BookmarkProvider<StartAtBookmark, StartAt>
    {
        public override async ValueTask<IEnumerable<IBookmark>> GetBookmarksAsync(BookmarkProviderContext<StartAt> context, CancellationToken cancellationToken)
        {
            var executeAt = await GetExecuteAtAsync(context, cancellationToken);

            if (executeAt != null)
                return new[]
                {
                    new StartAtBookmark
                    {
                        ExecuteAt = executeAt.Value,
                    }
                };
            
            return Enumerable.Empty<IBookmark>();
        }

        private static async Task<Instant?> GetExecuteAtAsync(BookmarkProviderContext<StartAt> context, CancellationToken cancellationToken) =>
            context.Mode == BookmarkIndexingMode.WorkflowInstance
                ? context.Activity.GetState(x => x.ExecuteAt)
                : await context.Activity.GetPropertyValueAsync(x => x.Instant, cancellationToken);
    }
}