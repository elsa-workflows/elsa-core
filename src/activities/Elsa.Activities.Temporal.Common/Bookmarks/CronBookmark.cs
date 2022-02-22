using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
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
        public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<Cron> context, CancellationToken cancellationToken)
        {
            var cronExpression = await context.ReadActivityPropertyAsync(x => x.CronExpression, cancellationToken);

            if (context.Mode == BookmarkIndexingMode.WorkflowInstance)
            {
                var executeAt = GetExecuteAt(context);

                if (executeAt == null)
                    return Enumerable.Empty<BookmarkResult>();

                return new[]
                {
                    Result(new CronBookmark
                    {
                        ExecuteAt = executeAt.Value,
                        CronExpression = cronExpression!
                    })
                };
            }

            return new[]
            {
                Result(new CronBookmark
                {
                    CronExpression = cronExpression!
                })
            };
        }

        private Instant? GetExecuteAt(BookmarkProviderContext<Cron> context) =>
            context.Mode == BookmarkIndexingMode.WorkflowInstance
                ? context.Activity.GetPropertyValue(x => x.ExecuteAt)
                : default;
    }
}