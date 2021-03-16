using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

        public override ValueTask<IEnumerable<IBookmark>> GetBookmarksAsync(BookmarkProviderContext context, CancellationToken cancellationToken)
        {
            var activityType = context.ActivityType;
            var eventType = (string)activityType.Attributes[NotificationActivityTypeProvider.EventTypeAttribute];
            var correlationId = context.ActivityExecutionContext.WorkflowExecutionContext.CorrelationId;
            var bookmarks = new[] { new NotificationBookmark(eventType, correlationId) };
            return new ValueTask<IEnumerable<IBookmark>>(bookmarks);
        }
    }
}