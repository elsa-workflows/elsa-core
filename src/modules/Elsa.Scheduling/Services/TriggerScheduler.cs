using System.Text.Json;
using Elsa.Extensions;
using Elsa.Scheduling.Activities;
using Elsa.Scheduling.Contracts;
using Elsa.Scheduling.Schedules;
using Elsa.Scheduling.Tasks;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Scheduling.Services;

/// <inheritdoc />
public class TriggerScheduler : ITriggerScheduler
{
    private const string RootGroupKey = "WorkflowDefinition";

    private readonly IScheduler _scheduler;

    /// <summary>
    /// Initializes a new instance of the <see cref="TriggerScheduler"/> class.
    /// </summary>
    public TriggerScheduler(IScheduler scheduler)
    {
        _scheduler = scheduler;
    }

    /// <inheritdoc />
    public async Task ScheduleAsync(IEnumerable<StoredTrigger> triggers, CancellationToken cancellationToken = default)
    {
        var triggerList = triggers.ToList();

        // Select all Timer triggers.
        var timerTriggers = triggerList.Filter<Activities.Timer>().ToList();
        var startAtTriggers = triggerList.Filter<StartAt>().ToList();

        // Schedule each Timer trigger.
        foreach (var trigger in timerTriggers)
        {
            var (startAt, interval) = JsonSerializer.Deserialize<TimerPayload>(trigger.Data!)!;
            var groupKeys = new[] { RootGroupKey, trigger.WorkflowDefinitionId };
            await _scheduler.ScheduleAsync(trigger.Id, new RunWorkflowTask(trigger.WorkflowDefinitionId), new RecurringSchedule(startAt, interval), groupKeys, cancellationToken);
        }

        // Schedule each StartAt trigger.
        foreach (var trigger in startAtTriggers)
        {
            var executeAt = JsonSerializer.Deserialize<StartAtPayload>(trigger.Data!)!.ExecuteAt;
            var groupKeys = new[] { RootGroupKey, trigger.WorkflowDefinitionId };
            var input = new { ExecuteAt = executeAt }.ToDictionary();
            await _scheduler.ScheduleAsync(trigger.WorkflowDefinitionId, new RunWorkflowTask(trigger.WorkflowDefinitionId, input), new SpecificInstantSchedule(executeAt), groupKeys, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task UnscheduleAsync(IEnumerable<StoredTrigger> triggers, CancellationToken cancellationToken = default)
    {
        var triggerList = triggers.ToList();

        // Select all Timer triggers.
        var timerTriggers = triggerList.Filter<Activities.Timer>().ToList();

        // Select all StartAt triggers.
        var startAtTriggers = triggerList.Filter<Activities.Timer>().ToList();

        // Unschedule all triggers for the distinct set of affected workflows.
        var workflowDefinitionIds = timerTriggers
            .Select(x => x.WorkflowDefinitionId)
            .Concat(startAtTriggers.Select(x => x.WorkflowDefinitionId))
            .Distinct()
            .ToList();

        foreach (var workflowDefinitionId in workflowDefinitionIds)
        {
            var groupKeys = new[] { RootGroupKey, workflowDefinitionId };
            await _scheduler.UnscheduleAsync(groupKeys, cancellationToken);
        }
    }
}