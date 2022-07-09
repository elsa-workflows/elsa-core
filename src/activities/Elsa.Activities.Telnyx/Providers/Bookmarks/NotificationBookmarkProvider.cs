using System.Collections.Generic;
using Elsa.Activities.Telnyx.Providers.ActivityTypes;
using Elsa.Services;

namespace Elsa.Activities.Telnyx.Providers.Bookmarks
{
    public record NotificationBookmark(string EventType) : IBookmark
    {
    }

    public class NotificationBookmarkProvider : BookmarkProvider<NotificationBookmark>
    {
        public override bool SupportsActivity(BookmarkProviderContext context)
        {
            var activityType = context.ActivityType;
            return activityType.Attributes.ContainsKey(NotificationActivityTypeProvider.NotificationAttribute);
        }

        public override IEnumerable<BookmarkResult> GetBookmarks(BookmarkProviderContext context)
        {
            var activityType = context.ActivityType;
            var eventType = (string) activityType.Attributes[NotificationActivityTypeProvider.EventTypeAttribute];
            return new[] {Result(new NotificationBookmark(eventType))};
        }
    }
}