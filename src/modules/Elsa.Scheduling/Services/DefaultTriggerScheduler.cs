using Elsa.Common.Contracts;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Scheduling.Activities;
using Elsa.Scheduling.Bookmarks;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Requests;
using Microsoft.Extensions.Logging;

namespace Elsa.Scheduling.Services;

/// <summary>
/// A default implementation of <see cref="ITriggerScheduler"/> that schedules triggers using <see cref="IWorkflowScheduler"/>.
/// </summary>
public class DefaultTriggerScheduler(IWorkflowScheduler workflowScheduler, ISystemClock systemClock, ILogger<DefaultTriggerScheduler> logger)
    : ITriggerScheduler
{
    /// <inheritdoc />
    public async Task ScheduleAsync(IEnumerable<StoredTrigger> triggers, CancellationToken cancellationToken = default)
    {
        var triggerList = triggers.ToList();
        var timerTriggers = triggerList.Filter<Activities.Timer>();
        var startAtTriggers = triggerList.Filter<StartAt>();
        var cronTriggers = triggerList.Filter<Cron>();
        var now = systemClock.UtcNow;

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
            await workflowScheduler.ScheduleRecurringAsync(trigger.Id, request, startAt, interval, cancellationToken);
        }

        // Schedule each StartAt trigger.
        foreach (var trigger in startAtTriggers)
        {
            var executeAt = trigger.GetPayload<StartAtPayload>().ExecuteAt;
            
            // If the trigger is in the past, log info and skip scheduling.
            if (executeAt < now)
            {
                logger.LogInformation("StartAt trigger is in the past. TriggerId: {TriggerId}. ExecuteAt: {ExecuteAt}. Skipping scheduling", trigger.Id, executeAt);
                continue;
            }
            
            var input = new { ExecuteAt = executeAt }.ToDictionary();
            var request = new ScheduleNewWorkflowInstanceRequest
            {
                WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionVersionId(trigger.WorkflowDefinitionVersionId),
                TriggerActivityId = trigger.ActivityId,
                Input = input
            };

            await workflowScheduler.ScheduleAtAsync(trigger.Id, request, executeAt, cancellationToken);
        }

        // Schedule each Cron trigger.
        foreach (var trigger in cronTriggers)
        {
            var payload = trigger.GetPayload<CronTriggerPayload>();
            var cronExpression = payload.CronExpression;

            if (string.IsNullOrWhiteSpace(cronExpression))
            {
                logger.LogWarning("Cron expression is empty. TriggerId: {TriggerId}. Skipping scheduling of this trigger", trigger.Id);
                continue;
            }
            
            var input = new { CronExpression = cronExpression }.ToDictionary();
            var request = new ScheduleNewWorkflowInstanceRequest
            {
                WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionVersionId(trigger.WorkflowDefinitionVersionId),
                TriggerActivityId = trigger.ActivityId,
                Input = input
            };
            try
            {
                await workflowScheduler.ScheduleCronAsync(trigger.Id, request, cronExpression, cancellationToken);
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
            await workflowScheduler.UnscheduleAsync(trigger.Id, cancellationToken);
    }
}
