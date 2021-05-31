using System.Collections.Generic;
using Elsa.Activities.Telnyx.ActivityTypes;
using Elsa.Bookmarks;

namespace Elsa.Activities.Telnyx.Bookmarks
{
    public record NotificationBookmark(string EventType, string? CorrelationId = default) : IBookmark
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
            var correlationId = context.ActivityExecutionContext.WorkflowExecutionContext.CorrelationId;
            return new[] { Result(new NotificationBookmark(eventType, correlationId)) };
        }
    }
}