using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Bookmarks;
using NodaTime;

namespace Elsa.Activities.Temporal.Common.Bookmarks
{
    public class TimerBookmark : IBookmark
    {
        public Instant ExecuteAt { get; set; }
        public Duration Interval { get; set; }
    }

    public class TimerBookmarkProvider : BookmarkProvider<TimerBookmark, Timer>
    {
        private readonly IClock _clock;

        public TimerBookmarkProvider(IClock clock)
        {
            _clock = clock;
        }

        public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<Timer> context, CancellationToken cancellationToken)
        {
            var interval = await context.ReadActivityPropertyAsync(x => x.Timeout, cancellationToken);
            var executeAt = GetExecuteAt(context, interval);

            if (executeAt != null)
                return new[]
                {
                    Result(new TimerBookmark
                    {
                        ExecuteAt = executeAt.Value,
                        Interval = interval
                    })
                };

            return Enumerable.Empty<BookmarkResult>();
        }

        private Instant? GetExecuteAt(BookmarkProviderContext<Timer> context, Duration interval) =>
            context.Mode == BookmarkIndexingMode.WorkflowInstance 
                ? context.Activity.GetPropertyValue(x => x.ExecuteAt) 
                : _clock.GetCurrentInstant().Plus(interval);
    }
}