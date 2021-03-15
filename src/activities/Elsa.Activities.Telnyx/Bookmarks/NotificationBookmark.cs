using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Activities;
using Elsa.ActivityProviders;
using Elsa.Bookmarks;
using Elsa.Services.Models;

namespace Elsa.Activities.Telnyx.Bookmarks
{
    public record NotificationBookmark(string EventType, string? CorrelationId = default) : IBookmark
    {
    }

    public class NotificationBookmarkProvider : BookmarkProvider<NotificationBookmark, TelnyxNotification>
    {
        private readonly IActivityTypeService _activityTypeService;

        public NotificationBookmarkProvider(IActivityTypeService activityTypeService)
        {
            _activityTypeService = activityTypeService;
        }
        
        public override async ValueTask<bool> SupportsActivityAsync(BookmarkProviderContext<TelnyxNotification> context, CancellationToken cancellationToken = default)
        {
            var activityBlueprint = context.ActivityExecutionContext.ActivityBlueprint;
            var activityType = await _activityTypeService.GetActivityTypeAsync(activityBlueprint.Type, cancellationToken);
            return activityType.Type == typeof(TelnyxNotification);
        }

        public override ValueTask<IEnumerable<IBookmark>> GetBookmarksAsync(BookmarkProviderContext<TelnyxNotification> context, CancellationToken cancellationToken)
        {
            var activityType = context.ActivityType;
            var eventType = (string)activityType.Attributes[nameof(TelnyxNotification.EventType)];
            var correlationId = context.ActivityExecutionContext.WorkflowExecutionContext.CorrelationId;
            var bookmarks = new[] { new NotificationBookmark(eventType, correlationId) };
            return new ValueTask<IEnumerable<IBookmark>>(bookmarks);
        }
    }
}