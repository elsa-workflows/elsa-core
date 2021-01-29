using System.Collections.Generic;
using Elsa.Bookmarks;
using Elsa.Models;
using NodaTime;

namespace Elsa.Activities.Timers.Triggers
{
    public class CronBookmark : IBookmark
    {
        public Instant ExecuteAt { get; set; }
    }
    
    public class CronBookmarkProvider : BookmarkProvider<CronBookmark, Cron>
    {
        public override IEnumerable<IBookmark> GetTriggers(BookmarkProviderContext<Cron> context)
        {
            var executeAt = context.Activity.GetState(x => x.ExecuteAt);

            if (executeAt != null)
                yield return new CronBookmark
                {
                    ExecuteAt = executeAt.Value,
                };
        }
    }
}