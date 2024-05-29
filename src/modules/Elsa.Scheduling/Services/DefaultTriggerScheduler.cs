using Elsa.Common.Contracts;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Scheduling.Activities;
using Elsa.Scheduling.Bookmarks;
using Elsa.Scheduling.Contracts;
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
        // Select Timer, StartAt and Cron triggers.
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
            var request = new DispatchWorkflowDefinitionRequest
            {
                DefinitionId = trigger.WorkflowDefinitionId,
                VersionOptions = VersionOptions.Published,
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
            var request = new DispatchWorkflowDefinitionRequest
            {
                DefinitionId = trigger.WorkflowDefinitionId,
                VersionOptions = VersionOptions.Published,
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
            var input = new { CronExpression = cronExpression }.ToDictionary();
            var request = new DispatchWorkflowDefinitionRequest
            {
                DefinitionId = trigger.WorkflowDefinitionId,
                VersionOptions = VersionOptions.Published,
                TriggerActivityId = trigger.ActivityId,
                Input = input
            };
            try
            {
                await workflowScheduler.ScheduleCronAsync(trigger.Id, request, cronExpression, cancellationToken);
            }
            catch (FormatException ex)
            {
                logger.LogWarning($"Cron expression format error: {ex.Message}. CronExpression: {cronExpression}");
            }
        }
    }

    /// <inheritdoc />
    public async Task UnscheduleAsync(IEnumerable<StoredTrigger> triggers, CancellationToken cancellationToken = default)
    {
        var triggerList = triggers.ToList();

        // Select all Timer triggers.
        var timerTriggers = triggerList.Filter<Activities.Timer>();

        // Select all StartAt triggers.
        var startAtTriggers = triggerList.Filter<StartAt>();

        // Select all Cron triggers.
        var cronTriggers = triggerList.Filter<Cron>();

        // Concatenate the filtered triggers.
        var filteredTriggers = timerTriggers.Concat(startAtTriggers).Concat(cronTriggers);

        // Unschedule each trigger.
        foreach (var trigger in filteredTriggers)
            await workflowScheduler.UnscheduleAsync(trigger.Id, cancellationToken);
    }
}
