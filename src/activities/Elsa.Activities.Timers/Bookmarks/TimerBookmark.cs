using System.Collections.Generic;
using Elsa.Bookmarks;
using Elsa.Models;
using NodaTime;

namespace Elsa.Activities.Timers.Triggers
{
    public class TimerBookmark : IBookmark
    {
        public Instant ExecuteAt { get; set; }
    }

    public class TimerBookmarkProvider : BookmarkProvider<TimerBookmark, Timer>
    {
        public override IEnumerable<IBookmark> GetTriggers(BookmarkProviderContext<Timer> context)
        {
            var executeAt = context.Activity.GetState(x => x.ExecuteAt);

            if (executeAt != null)
                yield return new TimerBookmark
                {
                    ExecuteAt = executeAt.Value,
                };
        }
    }
}