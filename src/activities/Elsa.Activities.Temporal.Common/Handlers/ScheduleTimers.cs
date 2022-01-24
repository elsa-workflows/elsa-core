using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Temporal.Common.Bookmarks;
using Elsa.Activities.Temporal.Common.Services;
using Elsa.Events;
using Elsa.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Elsa.Activities.Temporal.Common.Handlers
{
    public class ScheduleTimers : INotificationHandler<TriggerIndexingFinished>, INotificationHandler<BookmarkIndexingFinished>
    {
        // TODO: Design multi-tenancy. 
        private const string? TenantId = default;

        private readonly IWorkflowDefinitionScheduler _workflowDefinitionScheduler;
        private readonly IWorkflowInstanceScheduler _workflowInstanceScheduler;
        private readonly IBookmarkSerializer _bookmarkSerializer;
        private readonly ILogger<ScheduleTimers> _logger;

        public ScheduleTimers(
            IWorkflowDefinitionScheduler workflowDefinitionScheduler,
            IWorkflowInstanceScheduler workflowInstanceScheduler,
            IBookmarkSerializer bookmarkSerializer,
            ILogger<ScheduleTimers> logger)
        {
            _workflowDefinitionScheduler = workflowDefinitionScheduler;
            _workflowInstanceScheduler = workflowInstanceScheduler;
            _bookmarkSerializer = bookmarkSerializer;
            _logger = logger;
        }

        public async Task Handle(TriggerIndexingFinished notification, CancellationToken cancellationToken)
        {
            var startAtTriggers = notification.Triggers.FilterByBookmarkType<StartAtBookmark>();
            var timerTriggers = notification.Triggers.FilterByBookmarkType<TimerBookmark>();
            var cronTriggers = notification.Triggers.FilterByBookmarkType<CronBookmark>();

            await _workflowDefinitionScheduler.UnscheduleAsync(notification.WorkflowDefinitionId, cancellationToken);

            foreach (var trigger in startAtTriggers)
            {
                var bookmark = _bookmarkSerializer.Deserialize<StartAtBookmark>(trigger.Model);
                await Try(() => _workflowDefinitionScheduler.ScheduleAsync(trigger.WorkflowDefinitionId, trigger.ActivityId, bookmark.ExecuteAt, null, cancellationToken));
            }

            foreach (var trigger in timerTriggers)
            {
                var bookmark = _bookmarkSerializer.Deserialize<TimerBookmark>(trigger.Model);
                await Try(() => _workflowDefinitionScheduler.ScheduleAsync(trigger.WorkflowDefinitionId, trigger.ActivityId, bookmark.ExecuteAt, bookmark.Interval, cancellationToken));
            }

            foreach (var trigger in cronTriggers)
            {
                var bookmark = _bookmarkSerializer.Deserialize<CronBookmark>(trigger.Model);
                await Try(() => _workflowDefinitionScheduler.ScheduleAsync(trigger.WorkflowDefinitionId, trigger.ActivityId, bookmark.CronExpression, cancellationToken));
            }
        }

        public async Task Handle(BookmarkIndexingFinished notification, CancellationToken cancellationToken)
        {
            var bookmarks = notification.Bookmarks;
            var workflowInstanceId = notification.WorkflowInstanceId;
            var startAtBookmarks = bookmarks.FilterByType<StartAtBookmark>();
            var timerBookmarks = bookmarks.FilterByType<TimerBookmark>();
            var cronBookmarks = bookmarks.FilterByType<CronBookmark>();

            await _workflowInstanceScheduler.UnscheduleAsync(workflowInstanceId, cancellationToken);

            foreach (var trigger in startAtBookmarks)
            {
                var bookmark = _bookmarkSerializer.Deserialize<StartAtBookmark>(trigger.Model);
                await Try(() => _workflowInstanceScheduler.ScheduleAsync(workflowInstanceId, trigger.ActivityId, bookmark.ExecuteAt, null, cancellationToken));
            }

            foreach (var trigger in timerBookmarks)
            {
                var bookmark = _bookmarkSerializer.Deserialize<TimerBookmark>(trigger.Model);
                await Try(() => _workflowInstanceScheduler.ScheduleAsync(workflowInstanceId, trigger.ActivityId, bookmark.ExecuteAt, bookmark.Interval, cancellationToken));
            }

            foreach (var trigger in cronBookmarks)
            {
                var bookmark = _bookmarkSerializer.Deserialize<CronBookmark>(trigger.Model);
                await Try(() => _workflowInstanceScheduler.ScheduleAsync(workflowInstanceId, trigger.ActivityId, bookmark.CronExpression, cancellationToken));
            }
        }

        private async Task Try(Func<Task> operation)
        {
            try
            {
                await operation();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Can't schedule workflow");
            }
        }
    }
}