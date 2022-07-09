using System;
using System.Threading.Tasks;
using Elsa.Activities.Temporal.Common.Bookmarks;
using Elsa.Activities.Temporal.Common.Messages;
using Elsa.Activities.Temporal.Common.Services;
using Elsa.Services;
using Microsoft.Extensions.Logging;
using Rebus.Handlers;

namespace Elsa.Activities.Temporal.Common.Consumers;

public class ScheduleBookmarkConsumer : IHandleMessages<ScheduleTemporalBookmark>
{
    private readonly IBookmarkSerializer _bookmarkSerializer;
    private readonly IWorkflowInstanceScheduler _workflowInstanceScheduler;
    private readonly ILogger _logger;

    public ScheduleBookmarkConsumer(IBookmarkSerializer bookmarkSerializer, IWorkflowInstanceScheduler workflowInstanceScheduler, ILogger<ScheduleBookmarkConsumer> logger)
    {
        _bookmarkSerializer = bookmarkSerializer;
        _workflowInstanceScheduler = workflowInstanceScheduler;
        _logger = logger;
    }

    public async Task Handle(ScheduleTemporalBookmark message)
    {
        var bookmark = message.Bookmark;
        var bookmarkTypeName = bookmark.ModelType;
        var bookmarkType = Type.GetType(bookmarkTypeName)!;
        var model = _bookmarkSerializer.Deserialize(bookmark.Model, bookmarkType);

        _logger.LogDebug("Scheduling bookmark {@Bookmark}", model);

        switch (model)
        {
            case TimerBookmark timerBookmark:
                await _workflowInstanceScheduler.ScheduleAsync(bookmark.WorkflowInstanceId, bookmark.ActivityId, timerBookmark.ExecuteAt, null);
                break;
            case StartAtBookmark startAtBookmark:
                await _workflowInstanceScheduler.ScheduleAsync(bookmark.WorkflowInstanceId, bookmark.ActivityId, startAtBookmark.ExecuteAt, null);
                break;
            case CronBookmark cronBookmark:
                await _workflowInstanceScheduler.ScheduleAsync(bookmark.WorkflowInstanceId!, bookmark.ActivityId, cronBookmark.CronExpression);
                break;
        }
    }
}