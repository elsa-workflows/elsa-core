using System;
using System.Threading.Tasks;
using Elsa.Activities.Temporal.Common.Bookmarks;
using Elsa.Activities.Temporal.Common.Messages;
using Elsa.Activities.Temporal.Common.Services;
using Elsa.Services;
using Microsoft.Extensions.Logging;
using Rebus.Handlers;

namespace Elsa.Activities.Temporal.Common.Consumers;

public class ScheduleTriggerConsumer : IHandleMessages<ScheduleTemporalTrigger>
{
    private readonly IBookmarkSerializer _bookmarkSerializer;
    private readonly IWorkflowDefinitionScheduler _workflowDefinitionScheduler;
    private readonly ILogger _logger;

    public ScheduleTriggerConsumer(IBookmarkSerializer bookmarkSerializer, IWorkflowDefinitionScheduler workflowDefinitionScheduler, ILogger<ScheduleTriggerConsumer> logger)
    {
        _bookmarkSerializer = bookmarkSerializer;
        _workflowDefinitionScheduler = workflowDefinitionScheduler;
        _logger = logger;
    }

    public async Task Handle(ScheduleTemporalTrigger message)
    {
        var trigger = message.Trigger;
        var bookmarkTypeName = trigger.ModelType;
        var bookmarkType = Type.GetType(bookmarkTypeName)!;
        var model = _bookmarkSerializer.Deserialize(trigger.Model, bookmarkType);

        _logger.LogDebug("Scheduling trigger {@Trigger}", model);

        switch (model)
        {
            case TimerBookmark timerBookmark:
                await _workflowDefinitionScheduler.ScheduleAsync(trigger.WorkflowDefinitionId, trigger.ActivityId, timerBookmark.ExecuteAt, timerBookmark.Interval);
                break;
            case StartAtBookmark startAtBookmark:
                await _workflowDefinitionScheduler.ScheduleAsync(trigger.WorkflowDefinitionId, trigger.ActivityId, startAtBookmark.ExecuteAt, null);
                break;
            case CronBookmark cronBookmark:
                await _workflowDefinitionScheduler.ScheduleAsync(trigger.WorkflowDefinitionId!, trigger.ActivityId, cronBookmark.CronExpression);
                break;
        }
    }
}