using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Bookmarks;
using NodaTime;

namespace Elsa.Activities.Temporal.Common.Bookmarks
{
    public class CronBookmark : IBookmark
    {
        public Instant? ExecuteAt { get; set; }
        public string CronExpression { get; set; } = default!;
    }

    public class CronBookmarkProvider : BookmarkProvider<CronBookmark, Cron>
    {
        public override async ValueTask<IEnumerable<IBookmark>> GetBookmarksAsync(BookmarkProviderContext<Cron> context, CancellationToken cancellationToken)
        {
            var cronExpression = await context.Activity.GetPropertyValueAsync(x => x.CronExpression, cancellationToken);

            if (context.Mode == BookmarkIndexingMode.WorkflowInstance)
            {
                var executeAt = context.Activity.GetState(x => x.ExecuteAt);
                
                if(executeAt == null)
                    return Enumerable.Empty<IBookmark>();
                
                return new[]
                {
                    new CronBookmark
                    {
                        ExecuteAt = executeAt.Value,
                        CronExpression = cronExpression!
                    }
                };
            }

            return new[]
            {
                new CronBookmark
                {
                    CronExpression = cronExpression!
                }
            };
        }

        private Instant? GetExecuteAt(BookmarkProviderContext<Cron> context) =>
            context.Mode == BookmarkIndexingMode.WorkflowInstance
                ? context.Activity.GetState(x => x.ExecuteAt)
                : default;
    }
}