using System.Collections.Generic;
using Elsa.Bookmarks;
using NodaTime;

namespace Elsa.Activities.Timers.Bookmarks
{
    public class StartAtBookmark : IBookmark
    {
        public Instant ExecuteAt { get; set; }
    }

    public class StartAtBookmarkProvider : BookmarkProvider<StartAtBookmark, StartAt>
    {
        public override IEnumerable<IBookmark> GetTriggers(BookmarkProviderContext<StartAt> context)
        {
            var executeAt = context.Activity.GetState(x => x.ExecuteAt);

            if (executeAt != null)
                yield return new StartAtBookmark
                {
                    ExecuteAt = executeAt.Value,
                };
        }
    }
}