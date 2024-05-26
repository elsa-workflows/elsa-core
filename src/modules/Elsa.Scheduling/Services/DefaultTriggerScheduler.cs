using Elsa.Extensions;
using Elsa.Scheduling.Activities;
using Elsa.Scheduling.Bookmarks;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Entities;
using Microsoft.Extensions.Logging;

namespace Elsa.Scheduling.Services;

/// <summary>
/// A default implementation of <see cref="ITriggerScheduler"/> that schedules triggers using <see cref="IWorkflowScheduler"/>.
/// </summary>
public class DefaultTriggerScheduler : ITriggerScheduler
{
    private readonly IWorkflowScheduler _workflowScheduler;
    private readonly ILogger<DefaultTriggerScheduler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultTriggerScheduler"/> class.
    /// </summary>
    public DefaultTriggerScheduler(IWorkflowScheduler workflowScheduler, ILogger<DefaultTriggerScheduler> logger)
    {
        _workflowScheduler = workflowScheduler;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task ScheduleAsync(IEnumerable<StoredTrigger> triggers, CancellationToken cancellationToken = default)
    {
        var triggerList = triggers.ToList();
        var timerTriggers = triggerList.Filter<Activities.Timer>();
        var startAtTriggers = triggerList.Filter<StartAt>();
        var cronTriggers = triggerList.Filter<Cron>();

        // Schedule each Timer trigger.
        foreach (var trigger in timerTriggers)
        {
            var (startAt, interval) = trigger.GetPayload<TimerTriggerPayload>();
            var input = new { StartAt = startAt, Interval = interval }.ToDictionary();
            var request = new ScheduleNewWorkflowInstanceRequest
            {
                WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionVersionId(trigger.WorkflowDefinitionVersionId),
                TriggerActivityId = trigger.ActivityId,
                Input = input
            };
            await _workflowScheduler.ScheduleRecurringAsync(trigger.Id, request, startAt, interval, cancellationToken);
        }

        // Schedule each StartAt trigger.
        foreach (var trigger in startAtTriggers)
        {
            var executeAt = trigger.GetPayload<StartAtPayload>().ExecuteAt;
            var input = new { ExecuteAt = executeAt }.ToDictionary();
            var request = new ScheduleNewWorkflowInstanceRequest
            {
                WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionVersionId(trigger.WorkflowDefinitionVersionId),
                TriggerActivityId = trigger.ActivityId,
                Input = input
            };

            await _workflowScheduler.ScheduleAtAsync(trigger.Id, request, executeAt, cancellationToken);
        }

        // Schedule each Cron trigger.
        foreach (var trigger in cronTriggers)
        {
            var payload = trigger.GetPayload<CronTriggerPayload>();
            var cronExpression = payload.CronExpression;
            var input = new { CronExpression = cronExpression }.ToDictionary();
            var request = new ScheduleNewWorkflowInstanceRequest
            {
                WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionVersionId(trigger.WorkflowDefinitionVersionId),
                TriggerActivityId = trigger.ActivityId,
                Input = input
            };
            try
            {
                await _workflowScheduler.ScheduleCronAsync(trigger.Id, request, cronExpression, cancellationToken);
            }
            catch (FormatException ex)
            {
                _logger.LogWarning("Cron expression format error: {ExMessage}. CronExpression: {CronExpression}", ex.Message, cronExpression);
            }
        }
    }

    /// <inheritdoc />
    public async Task UnscheduleAsync(IEnumerable<StoredTrigger> triggers, CancellationToken cancellationToken = default)
    {
        var triggerList = triggers.ToList();
        var timerTriggers = triggerList.Filter<Activities.Timer>();
        var startAtTriggers = triggerList.Filter<StartAt>();
        var cronTriggers = triggerList.Filter<Cron>();
        var filteredTriggers = timerTriggers.Concat(startAtTriggers).Concat(cronTriggers);
        
        foreach (var trigger in filteredTriggers)
            await _workflowScheduler.UnscheduleAsync(trigger.Id, cancellationToken);
    }
}
