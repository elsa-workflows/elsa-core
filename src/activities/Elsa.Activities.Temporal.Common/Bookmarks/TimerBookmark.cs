using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Temporal.Common.Options;
using Elsa.Attributes;
using Elsa.Bookmarks;
using NodaTime;

namespace Elsa.Activities.Temporal.Common.Bookmarks
{
    public class TimerBookmark : IBookmark
    {
        public Instant ExecuteAt { get; set; }
        public Duration Interval { get; set; }
        
        [ExcludeFromHash]
        public ClusterMode? ClusterMode { get; set; }
    }

    public class TimerBookmarkProvider : BookmarkProvider<TimerBookmark, Timer>
    {
        private readonly IClock _clock;

        public TimerBookmarkProvider(IClock clock)
        {
            _clock = clock;
        }

        public override async ValueTask<IEnumerable<IBookmark>> GetBookmarksAsync(BookmarkProviderContext<Timer> context, CancellationToken cancellationToken)
        {
            var interval = await context.Activity.GetPropertyValueAsync(x => x.Timeout, cancellationToken);
            var clusterMode = context.Mode == BookmarkIndexingMode.WorkflowBlueprint ? await context.Activity.GetPropertyValueAsync(x => x.ClusterMode, cancellationToken) : default(ClusterMode?);
            var executeAt = GetExecuteAt(context, interval);

            if (executeAt != null)
                return new[]
                {
                    new TimerBookmark
                    {
                        ExecuteAt = executeAt.Value,
                        Interval = interval,
                        ClusterMode = clusterMode
                    }
                };

            return Enumerable.Empty<IBookmark>();
        }

        private Instant? GetExecuteAt(BookmarkProviderContext<Timer> context, Duration interval) =>
            context.Mode == BookmarkIndexingMode.WorkflowInstance 
                ? context.Activity.GetState(x => x.ExecuteAt) 
                : _clock.GetCurrentInstant().Plus(interval);
    }
}